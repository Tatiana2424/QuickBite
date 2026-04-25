using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QuickBite.BuildingBlocks.Common;

public sealed class DatabaseInitializationOptions
{
    public bool ApplyMigrationsOnStartup { get; set; }
    public bool SeedDemoData { get; set; }
    public bool RecreateDatabaseIfMigrationHistoryMissing { get; set; }
    public int MigrationRetryCount { get; set; } = 5;
    public int MigrationRetryDelaySeconds { get; set; } = 3;
}

public sealed class DatabaseInitializationOptionsValidator : IValidateOptions<DatabaseInitializationOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseInitializationOptions options)
    {
        var failures = new List<string>();
        if (options.MigrationRetryCount <= 0)
        {
            failures.Add("DatabaseInitialization:MigrationRetryCount must be greater than zero.");
        }

        if (options.MigrationRetryDelaySeconds < 0)
        {
            failures.Add("DatabaseInitialization:MigrationRetryDelaySeconds cannot be negative.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

public static class DatabaseInitializationExtensions
{
    public static IServiceCollection AddDatabaseInitializationOptions(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<DatabaseInitializationOptions>, DatabaseInitializationOptionsValidator>();
        services.AddOptions<DatabaseInitializationOptions>()
            .Bind(configuration.GetSection("DatabaseInitialization"))
            .ValidateOnStart();

        return services;
    }

    public static async Task InitializeDatabaseAsync<TContext>(
        this IServiceProvider serviceProvider,
        Func<TContext, DatabaseInitializationOptions, CancellationToken, Task>? seedAsync = null,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var options = scope.ServiceProvider.GetService<IOptions<DatabaseInitializationOptions>>()?.Value
            ?? new DatabaseInitializationOptions();

        if (options.ApplyMigrationsOnStartup)
        {
            await ExecuteWithRetryAsync(
                async ct =>
                {
                    await ResetLegacyDevelopmentDatabaseIfNeededAsync(dbContext, options, ct);
                    await dbContext.Database.MigrateAsync(ct);
                },
                options,
                cancellationToken);
        }

        if (seedAsync is not null)
        {
            await seedAsync(dbContext, options, cancellationToken);
        }
    }

    private static async Task ResetLegacyDevelopmentDatabaseIfNeededAsync<TContext>(
        TContext dbContext,
        DatabaseInitializationOptions options,
        CancellationToken cancellationToken)
        where TContext : DbContext
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return;
        }

        var migrationHistoryExists = await TableExistsAsync(dbContext, "__EFMigrationsHistory", cancellationToken);
        if (migrationHistoryExists)
        {
            return;
        }

        var userTableCount = await GetUserTableCountAsync(dbContext, cancellationToken);
        if (userTableCount == 0)
        {
            return;
        }

        if (!options.RecreateDatabaseIfMigrationHistoryMissing)
        {
            throw new InvalidOperationException(
                $"The database for {typeof(TContext).Name} was created without EF Core migrations history. " +
                "Enable DatabaseInitialization:RecreateDatabaseIfMigrationHistoryMissing for local development, " +
                "or recreate the database before applying migrations.");
        }

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync<TContext>(
        TContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
        where TContext : DbContext
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                  AND TABLE_NAME = @tableName
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            return result > 0;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<int> GetUserTableCountAsync<TContext>(TContext dbContext, CancellationToken cancellationToken)
        where TContext : DbContext
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                  AND TABLE_NAME <> '__EFMigrationsHistory'
                """;

            return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task ExecuteWithRetryAsync(
        Func<CancellationToken, Task> operation,
        DatabaseInitializationOptions options,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= options.MigrationRetryCount; attempt++)
        {
            try
            {
                await operation(cancellationToken);
                return;
            }
            catch (Exception exception) when (exception is DbException or InvalidOperationException)
            {
                lastException = exception;
                if (attempt == options.MigrationRetryCount)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(options.MigrationRetryDelaySeconds), cancellationToken);
            }
        }

        throw new InvalidOperationException("Database initialization failed after the configured retry count.", lastException);
    }
}

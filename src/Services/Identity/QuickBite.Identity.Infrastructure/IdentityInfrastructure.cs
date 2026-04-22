using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuickBite.BuildingBlocks.Common;
using QuickBite.Identity.Application;
using QuickBite.Identity.Domain;

namespace QuickBite.Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
        });
    }
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "QuickBite";
    public string Audience { get; set; } = "QuickBite.Web";
    public string Key { get; set; } = "quickbite-super-secret-development-key-change-me";
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            missing.Add(nameof(options.Issuer));
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            missing.Add(nameof(options.Audience));
        }

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            missing.Add(nameof(options.Key));
        }
        else if (options.Key.Length < 32)
        {
            return ValidateOptionsResult.Fail("Jwt:Key must be at least 32 characters long.");
        }

        return missing.Count > 0
            ? ValidateOptionsResult.Fail($"Jwt configuration is incomplete. Missing: {string.Join(", ", missing)}.")
            : ValidateOptionsResult.Success;
    }
}

public static class IdentityInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConfigurationGuard.GetRequiredConnectionString(configuration, "DefaultConnection");

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateOnStart();
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static async Task EnsureIdentityDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}

internal sealed class AuthService(IdentityDbContext dbContext, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new InvalidOperationException("A user with that email already exists.");
        }

        var user = new User(normalizedEmail, request.FullName, HashPassword(request.Password));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(user.Id, user.Email, user.FullName, CreateToken(user));
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var passwordHash = HashPassword(request.Password);

        var user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Email == normalizedEmail && x.PasswordHash == passwordHash,
            cancellationToken);

        return user is null ? null : new AuthResponse(user.Id, user.Email, user.FullName, CreateToken(user));
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private string CreateToken(User user)
    {
        var options = jwtOptions.Value;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

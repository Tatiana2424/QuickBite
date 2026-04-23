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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
            entity.HasMany(x => x.UserRoles).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            entity.HasMany(x => x.RefreshTokens).WithOne(x => x.User).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(200);
            entity.HasMany(x => x.UserRoles).WithOne(x => x.Role).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(512);
            entity.HasIndex(x => x.TokenHash).IsUnique();
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
        services.AddOptions<DatabaseInitializationOptions>()
            .Bind(configuration.GetSection("DatabaseInitialization"));
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
        await serviceProvider.InitializeDatabaseAsync<IdentityDbContext>(SeedAsync);
    }

    private static async Task SeedAsync(
        IdentityDbContext dbContext,
        DatabaseInitializationOptions options,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new Role(IdentityRoles.Customer, "Default customer role for ordering food."),
            new Role(IdentityRoles.RestaurantAdmin, "Restaurant managers responsible for menus."),
            new Role(IdentityRoles.Courier, "Courier role for delivery operations."),
            new Role(IdentityRoles.PlatformAdmin, "Platform administrators with elevated access.")
        };

        foreach (var role in roles.Where(role => dbContext.Roles.All(existing => existing.Name != role.Name)))
        {
            dbContext.Roles.Add(role);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!options.SeedDemoData || await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var customerRole = await dbContext.Roles.SingleAsync(x => x.Name == IdentityRoles.Customer, cancellationToken);
        var adminRole = await dbContext.Roles.SingleAsync(x => x.Name == IdentityRoles.PlatformAdmin, cancellationToken);

        var demoCustomer = new User("customer@quickbite.local", "QuickBite Customer", PasswordHasher.Hash("Pass123!"));
        demoCustomer.AssignRole(customerRole);

        var demoAdmin = new User("admin@quickbite.local", "QuickBite Admin", PasswordHasher.Hash("Pass123!"));
        demoAdmin.AssignRole(adminRole);

        dbContext.Users.AddRange(demoCustomer, demoAdmin);
        await dbContext.SaveChangesAsync(cancellationToken);
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
        var customerRole = await dbContext.Roles.SingleAsync(x => x.Name == IdentityRoles.Customer, cancellationToken);
        user.AssignRole(customerRole);

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

        if (user is not null)
        {
            await dbContext.Entry(user)
                .Collection(x => x.UserRoles)
                .Query()
                .Include(x => x.Role)
                .LoadAsync(cancellationToken);
        }

        return user is null ? null : new AuthResponse(user.Id, user.Email, user.FullName, CreateToken(user));
    }

    private static string HashPassword(string password) => PasswordHasher.Hash(password);

    private string CreateToken(User user)
    {
        var options = jwtOptions.Value;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName)
        };

        claims.AddRange(
            user.UserRoles
                .Where(x => x.Role is not null)
                .Select(x => new Claim(ClaimTypes.Role, x.Role!.Name)));

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

internal static class PasswordHasher
{
    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

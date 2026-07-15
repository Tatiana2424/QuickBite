using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Identity.Domain;

public sealed class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public List<UserRole> UserRoles { get; private set; } = new();
    public List<RefreshToken> RefreshTokens { get; private set; } = new();
    public List<CustomerAddress> Addresses { get; private set; } = new();

    private User()
    {
    }

    public User(string email, string fullName, string passwordHash, Guid? id = null)
    {
        if (id.HasValue)
        {
            UseSeedIdentity(id.Value);
        }

        Email = email.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        PasswordHash = passwordHash;
    }

    public void AssignRole(Role role)
    {
        if (UserRoles.Any(x => x.RoleId == role.Id))
        {
            return;
        }

        UserRoles.Add(new UserRole(Id, role));
    }
}

public sealed class CustomerAddress : Entity
{
    public Guid UserId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public User? User { get; private set; }

    private CustomerAddress()
    {
    }

    public CustomerAddress(
        Guid userId,
        string label,
        string line1,
        string? line2,
        string city,
        string state,
        string postalCode,
        string country,
        bool isDefault)
    {
        UserId = userId;
        Update(label, line1, line2, city, state, postalCode, country, isDefault);
    }

    public void Update(
        string label,
        string line1,
        string? line2,
        string city,
        string state,
        string postalCode,
        string country,
        bool isDefault)
    {
        Label = label.Trim();
        Line1 = line1.Trim();
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
        IsDefault = isDefault;
        Touch();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        Touch();
    }
}

public sealed class Role : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public List<UserRole> UserRoles { get; private set; } = new();

    private Role()
    {
    }

    public Role(string name, string description)
    {
        Name = name.Trim();
        Description = description.Trim();
    }
}

public sealed class UserRole : Entity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public User? User { get; private set; }
    public Role? Role { get; private set; }

    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public UserRole(Guid userId, Role role)
    {
        UserId = userId;
        RoleId = role.Id;
        Role = role;
    }
}

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public User? User { get; private set; }
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken()
    {
    }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
        Touch();
    }
}

public static class IdentityRoles
{
    public const string Customer = "Customer";
    public const string RestaurantAdmin = "RestaurantAdmin";
    public const string Courier = "Courier";
    public const string PlatformAdmin = "PlatformAdmin";
}

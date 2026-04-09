using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Identity.Domain;

public sealed class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    private User()
    {
    }

    public User(string email, string fullName, string passwordHash)
    {
        Email = email.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        PasswordHash = passwordHash;
    }
}

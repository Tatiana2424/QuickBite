using QuickBite.Identity.Domain;

namespace QuickBite.Identity.Tests;

public sealed class IdentityDomainTests
{
    [Fact]
    public void User_normalizes_email_and_assigns_roles_idempotently()
    {
        var user = new User(" DEMO@QUICKBITE.LOCAL ", " Demo User ", "hash");
        var role = new Role(IdentityRoles.Customer, "Can order food.");

        user.AssignRole(role);
        user.AssignRole(role);

        Assert.Equal("demo@quickbite.local", user.Email);
        Assert.Equal("Demo User", user.FullName);
        Assert.Single(user.UserRoles);
        Assert.Equal(role.Id, user.UserRoles[0].RoleId);
    }
}

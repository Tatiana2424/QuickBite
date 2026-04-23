using QuickBite.Identity.Domain;

namespace QuickBite.Identity.Tests;

public sealed class IdentityRoleTests
{
    [Fact]
    public void AssignRole_adds_role_once()
    {
        var user = new User("customer@example.com", "Customer Example", "HASH");
        var customerRole = new Role(IdentityRoles.Customer, "Default role");

        user.AssignRole(customerRole);
        user.AssignRole(customerRole);

        Assert.Single(user.UserRoles);
        Assert.Equal(customerRole.Id, user.UserRoles[0].RoleId);
    }
}

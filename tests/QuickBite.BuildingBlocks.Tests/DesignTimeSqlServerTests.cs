using QuickBite.BuildingBlocks.Common;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class DesignTimeSqlServerTests
{
    [Fact]
    public void ResolveConnectionString_returns_localdb_by_default()
    {
        Environment.SetEnvironmentVariable("QUICKBITE_DESIGNTIME_SQLSERVER", null);

        var connectionString = DesignTimeSqlServer.ResolveConnectionString("QuickBiteIdentityDb");

        Assert.Contains("(localdb)\\MSSQLLocalDB", connectionString);
        Assert.Contains("Database=QuickBiteIdentityDb", connectionString);
    }

    [Fact]
    public void ResolveConnectionString_uses_configured_server_when_environment_variable_is_present()
    {
        Environment.SetEnvironmentVariable("QUICKBITE_DESIGNTIME_SQLSERVER", "Server=localhost\\SQLEXPRESS01;Integrated Security=true");

        try
        {
            var connectionString = DesignTimeSqlServer.ResolveConnectionString("QuickBiteOrdersDb");

            Assert.Contains("Server=localhost\\SQLEXPRESS01", connectionString);
            Assert.Contains("Database=QuickBiteOrdersDb", connectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("QUICKBITE_DESIGNTIME_SQLSERVER", null);
        }
    }
}

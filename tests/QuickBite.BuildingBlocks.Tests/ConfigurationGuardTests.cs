using Microsoft.Extensions.Configuration;
using QuickBite.BuildingBlocks.Common;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class ConfigurationGuardTests
{
    [Fact]
    public void GetRequiredConnectionString_returns_value_when_present()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=QuickBite;"
            })
            .Build();

        var value = ConfigurationGuard.GetRequiredConnectionString(configuration, "DefaultConnection");

        Assert.Equal("Server=localhost;Database=QuickBite;", value);
    }

    [Fact]
    public void GetRequiredConnectionString_throws_when_missing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var action = () => ConfigurationGuard.GetRequiredConnectionString(configuration, "DefaultConnection");

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("DefaultConnection", exception.Message);
    }

    [Fact]
    public void GetRequiredValue_throws_when_missing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var action = () => ConfigurationGuard.GetRequiredValue(configuration, "Jwt:Key");

        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Jwt:Key", exception.Message);
    }
}

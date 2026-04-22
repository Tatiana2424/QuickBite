using Microsoft.Extensions.Configuration;

namespace QuickBite.BuildingBlocks.Common;

public static class ConfigurationGuard
{
    public static string GetRequiredConnectionString(IConfiguration configuration, string connectionStringName)
    {
        var value = configuration.GetConnectionString(connectionStringName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Missing required connection string '{connectionStringName}'.")
            : value;
    }

    public static string GetRequiredValue(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Missing required configuration value '{key}'.")
            : value;
    }
}

namespace QuickBite.BuildingBlocks.Common;

public static class DesignTimeSqlServer
{
    private const string DesignTimeConnectionEnvironmentVariable = "QUICKBITE_DESIGNTIME_SQLSERVER";

    public static string ResolveConnectionString(string databaseName)
    {
        var configuredServerConnection = Environment.GetEnvironmentVariable(DesignTimeConnectionEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredServerConnection))
        {
            return $"{configuredServerConnection.TrimEnd(';')};Database={databaseName};TrustServerCertificate=True";
        }

        return $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=True";
    }
}

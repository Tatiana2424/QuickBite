using System.Text.Json;

namespace QuickBite.QualityGates.Tests;

public sealed class GatewayContractTests
{
    [Theory]
    [InlineData("identity-route", "/identity/{**catch-all}", "identity-cluster")]
    [InlineData("catalog-route", "/catalog/{**catch-all}", "catalog-cluster")]
    [InlineData("orders-route", "/orders/{**catch-all}", "orders-cluster")]
    [InlineData("payments-route", "/payments/{**catch-all}", "payments-cluster")]
    [InlineData("delivery-route", "/delivery/{**catch-all}", "delivery-cluster")]
    public void Gateway_routes_keep_public_prefixes_and_strip_them_before_proxying(
        string routeName,
        string publicPath,
        string clusterName)
    {
        using var document = LoadGatewaySettings();
        var route = document.RootElement
            .GetProperty("ReverseProxy")
            .GetProperty("Routes")
            .GetProperty(routeName);

        Assert.Equal(publicPath, route.GetProperty("Match").GetProperty("Path").GetString());
        Assert.Equal(clusterName, route.GetProperty("ClusterId").GetString());
        Assert.Equal("/{**catch-all}", route.GetProperty("Transforms")[0].GetProperty("PathPattern").GetString());
    }

    [Theory]
    [InlineData("identity-cluster", "http://quickbite.identity.api:8080/")]
    [InlineData("catalog-cluster", "http://quickbite.catalog.api:8080/")]
    [InlineData("orders-cluster", "http://quickbite.orders.api:8080/")]
    [InlineData("payments-cluster", "http://quickbite.payments.api:8080/")]
    [InlineData("delivery-cluster", "http://quickbite.delivery.api:8080/")]
    public void Gateway_clusters_have_container_network_destinations_and_timeouts(string clusterName, string address)
    {
        using var document = LoadGatewaySettings();
        var cluster = document.RootElement
            .GetProperty("ReverseProxy")
            .GetProperty("Clusters")
            .GetProperty(clusterName);

        Assert.Equal(address, cluster.GetProperty("Destinations").GetProperty("primary").GetProperty("Address").GetString());
        Assert.False(string.IsNullOrWhiteSpace(cluster.GetProperty("HttpRequest").GetProperty("ActivityTimeout").GetString()));
    }

    private static JsonDocument LoadGatewaySettings()
    {
        var path = RepositoryPaths.File("src", "Gateway", "QuickBite.Gateway", "appsettings.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}

using Microsoft.Extensions.Configuration;
using QuickBite.BuildingBlocks.Api;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class ReverseProxyConfigurationValidatorTests
{
    [Fact]
    public void Validate_succeeds_for_complete_reverse_proxy_configuration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReverseProxy:Routes:orders-route:ClusterId"] = "orders-cluster",
            ["ReverseProxy:Routes:orders-route:Match:Path"] = "/orders/{**catch-all}",
            ["ReverseProxy:Clusters:orders-cluster:HttpRequest:ActivityTimeout"] = "00:00:20",
            ["ReverseProxy:Clusters:orders-cluster:Destinations:primary:Address"] = "http://localhost:5003/"
        });

        ReverseProxyConfigurationValidator.Validate(configuration);
    }

    [Fact]
    public void Validate_fails_when_route_references_missing_cluster()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReverseProxy:Routes:orders-route:ClusterId"] = "orders-cluster",
            ["ReverseProxy:Routes:orders-route:Match:Path"] = "/orders/{**catch-all}",
            ["ReverseProxy:Clusters:catalog-cluster:HttpRequest:ActivityTimeout"] = "00:00:15",
            ["ReverseProxy:Clusters:catalog-cluster:Destinations:primary:Address"] = "http://localhost:5002/"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ReverseProxyConfigurationValidator.Validate(configuration));

        Assert.Contains("missing cluster", exception.Message);
    }

    [Fact]
    public void Validate_fails_when_cluster_timeout_is_missing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReverseProxy:Routes:orders-route:ClusterId"] = "orders-cluster",
            ["ReverseProxy:Routes:orders-route:Match:Path"] = "/orders/{**catch-all}",
            ["ReverseProxy:Clusters:orders-cluster:Destinations:primary:Address"] = "http://localhost:5003/"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ReverseProxyConfigurationValidator.Validate(configuration));

        Assert.Contains("ActivityTimeout", exception.Message);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

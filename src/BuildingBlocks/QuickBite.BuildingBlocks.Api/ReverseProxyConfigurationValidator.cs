using Microsoft.Extensions.Configuration;

namespace QuickBite.BuildingBlocks.Api;

public static class ReverseProxyConfigurationValidator
{
    public static void Validate(IConfiguration configuration)
    {
        var reverseProxy = configuration.GetSection("ReverseProxy");
        if (!reverseProxy.Exists())
        {
            throw new InvalidOperationException("Missing required ReverseProxy configuration section.");
        }

        var routes = reverseProxy.GetSection("Routes").GetChildren().ToArray();
        var clusters = reverseProxy.GetSection("Clusters").GetChildren().ToArray();

        if (routes.Length == 0)
        {
            throw new InvalidOperationException("ReverseProxy:Routes must define at least one route.");
        }

        if (clusters.Length == 0)
        {
            throw new InvalidOperationException("ReverseProxy:Clusters must define at least one cluster.");
        }

        var clusterIds = clusters.Select(cluster => cluster.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var route in routes)
        {
            ValidateRoute(route, clusterIds);
        }

        foreach (var cluster in clusters)
        {
            ValidateCluster(cluster);
        }
    }

    private static void ValidateRoute(IConfigurationSection route, ISet<string> clusterIds)
    {
        var clusterId = route["ClusterId"];
        if (string.IsNullOrWhiteSpace(clusterId))
        {
            throw new InvalidOperationException($"ReverseProxy:Routes:{route.Key}:ClusterId must be configured.");
        }

        if (!clusterIds.Contains(clusterId))
        {
            throw new InvalidOperationException($"ReverseProxy route '{route.Key}' references missing cluster '{clusterId}'.");
        }

        var path = route.GetSection("Match")["Path"];
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
        {
            throw new InvalidOperationException($"ReverseProxy:Routes:{route.Key}:Match:Path must start with '/'.");
        }
    }

    private static void ValidateCluster(IConfigurationSection cluster)
    {
        var destinations = cluster.GetSection("Destinations").GetChildren().ToArray();
        if (destinations.Length == 0)
        {
            throw new InvalidOperationException($"ReverseProxy:Clusters:{cluster.Key}:Destinations must define at least one destination.");
        }

        foreach (var destination in destinations)
        {
            var address = destination["Address"];
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    $"ReverseProxy:Clusters:{cluster.Key}:Destinations:{destination.Key}:Address must be an absolute HTTP(S) URI.");
            }
        }

        var timeout = cluster.GetSection("HttpRequest")["ActivityTimeout"];
        if (string.IsNullOrWhiteSpace(timeout) || !TimeSpan.TryParse(timeout, out var parsedTimeout) || parsedTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"ReverseProxy:Clusters:{cluster.Key}:HttpRequest:ActivityTimeout must be a positive TimeSpan.");
        }
    }
}

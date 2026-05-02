using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Observability;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class ObservabilityTests
{
    [Fact]
    public void QuickBiteTelemetry_tags_require_key_value_pairs()
    {
        Assert.Throws<ArgumentException>(() => QuickBiteTelemetry.Tags("topic"));
    }

    [Fact]
    public void QuickBiteTelemetry_tags_build_key_value_pairs()
    {
        var tags = QuickBiteTelemetry.Tags("topic", "orders", "consumer.group", "payments");

        Assert.Equal("topic", tags[0].Key);
        Assert.Equal("orders", tags[0].Value);
        Assert.Equal("consumer.group", tags[1].Key);
        Assert.Equal("payments", tags[1].Value);
    }

    [Fact]
    public void QuickBite_health_options_keep_supplied_predicate()
    {
        var options = ObservabilityExtensions.QuickBiteHealthCheckOptions(registration => registration.Tags.Contains("ready"));

        Assert.NotNull(options.Predicate);
        Assert.True(options.Predicate!(new("db", _ => throw new NotImplementedException(), null, ["ready"])));
        Assert.False(options.Predicate!(new("live", _ => throw new NotImplementedException(), null, [])));
    }
}

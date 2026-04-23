using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Kafka;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class KafkaOptionsValidatorTests
{
    private readonly KafkaOptionsValidator _validator = new();

    [Fact]
    public void Validate_returns_success_for_complete_options()
    {
        var result = _validator.Validate(Options.DefaultName, new KafkaOptions
        {
            Enabled = true,
            BootstrapServers = "localhost:9092",
            Topics = new KafkaTopics
            {
                OrderCreated = "orders.created",
                PaymentSucceeded = "payments.succeeded",
                PaymentFailed = "payments.failed",
                DeliveryAssigned = "delivery.assigned",
                DeliveryCompleted = "delivery.completed"
            }
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_returns_success_when_kafka_is_disabled()
    {
        var result = _validator.Validate(Options.DefaultName, new KafkaOptions
        {
            Enabled = false,
            BootstrapServers = "",
            Topics = new KafkaTopics()
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_fails_when_bootstrap_servers_is_missing()
    {
        var result = _validator.Validate(Options.DefaultName, new KafkaOptions
        {
            Enabled = true,
            BootstrapServers = ""
        });

        Assert.False(result.Succeeded);
        Assert.Contains("BootstrapServers", result.FailureMessage);
    }

    [Fact]
    public void Validate_fails_when_any_topic_is_missing()
    {
        var result = _validator.Validate(Options.DefaultName, new KafkaOptions
        {
            Enabled = true,
            BootstrapServers = "localhost:9092",
            Topics = new KafkaTopics
            {
                OrderCreated = "orders.created",
                PaymentSucceeded = "payments.succeeded",
                PaymentFailed = "payments.failed",
                DeliveryAssigned = "",
                DeliveryCompleted = "delivery.completed"
            }
        });

        Assert.False(result.Succeeded);
        Assert.Contains(nameof(KafkaTopics.DeliveryAssigned), result.FailureMessage);
    }
}

using System.Text.Json;
using QuickBite.BuildingBlocks.Contracts;

namespace QuickBite.QualityGates.Tests;

public sealed class KafkaContractTests
{
    [Fact]
    public void Kafka_topic_names_are_consistent_across_event_driven_services()
    {
        var orders = LoadKafkaTopics("Orders");
        var payments = LoadKafkaTopics("Payments");
        var delivery = LoadKafkaTopics("Delivery");

        Assert.Equal(orders, payments);
        Assert.Equal(orders, delivery);
    }

    [Fact]
    public void Kafka_topics_are_unique_versioned_and_have_dead_letter_enabled()
    {
        var topicSettings = LoadKafkaSettings("Orders");
        var topics = topicSettings.GetProperty("Topics")
            .EnumerateObject()
            .Select(topic => topic.Value.GetString())
            .ToArray();

        Assert.All(topics, topic => Assert.EndsWith(".v1", topic));
        Assert.Equal(topics.Length, topics.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.True(topicSettings.GetProperty("DeadLetter").GetProperty("Enabled").GetBoolean());
        Assert.Equal(".dlq", topicSettings.GetProperty("DeadLetter").GetProperty("TopicSuffix").GetString());
        Assert.True(topicSettings.GetProperty("Consumer").GetProperty("MaxRetryAttempts").GetInt32() > 0);
    }

    [Fact]
    public void Integration_event_contracts_keep_expected_event_type_names()
    {
        Assert.Equal("order.created", new OrderCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), 10m, Array.Empty<OrderCreatedItem>()).EventType);
        Assert.Equal("payment.succeeded", new PaymentSucceededEvent(Guid.NewGuid(), Guid.NewGuid(), 10m).EventType);
        Assert.Equal("payment.failed", new PaymentFailedEvent(Guid.NewGuid(), Guid.NewGuid(), 10m, "Declined").EventType);
        Assert.Equal("delivery.assigned", new DeliveryAssignedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Mia").EventType);
        Assert.Equal("delivery.completed", new DeliveryCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow).EventType);
    }

    private static Dictionary<string, string> LoadKafkaTopics(string serviceName)
    {
        return LoadKafkaSettings(serviceName)
            .GetProperty("Topics")
            .EnumerateObject()
            .ToDictionary(
                topic => topic.Name,
                topic => topic.Value.GetString() ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static JsonElement LoadKafkaSettings(string serviceName)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ServiceSettingsPath(serviceName)));
        return document.RootElement.GetProperty("Kafka").Clone();
    }

    private static string ServiceSettingsPath(string serviceName)
    {
        return RepositoryPaths.File(
            "src",
            "Services",
            serviceName,
            $"QuickBite.{serviceName}.Api",
            "appsettings.json");
    }
}

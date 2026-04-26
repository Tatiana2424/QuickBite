using System.Text.Json;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Contracts;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class ReliabilityMessageTests
{
    [Fact]
    public void OutboxMessage_create_stores_durable_envelope_metadata()
    {
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var message = OutboxMessage.Create(
            "quickbite.orders.order-created.v1",
            new OrderCreatedEvent(orderId, Guid.NewGuid(), 42.50m, []),
            "QuickBite.Orders.Api",
            correlationId,
            causationId);

        Assert.Equal("quickbite.orders.order-created.v1", message.TopicName);
        Assert.Equal(orderId.ToString("N"), message.MessageKey);
        Assert.Equal(IntegrationEventTypes.OrderCreated, message.EventType);
        Assert.Equal(1, message.EventVersion);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.Equal(causationId, message.CausationId);
        Assert.Equal("QuickBite.Orders.Api", message.Producer);
        Assert.Null(message.PublishedAtUtc);

        using var document = JsonDocument.Parse(message.EnvelopeJson);
        Assert.Equal(message.EventId, document.RootElement.GetProperty("eventId").GetGuid());
        Assert.Equal(orderId, document.RootElement.GetProperty("payload").GetProperty("orderId").GetGuid());
    }

    [Fact]
    public void OutboxMessage_mark_failed_tracks_retry_state()
    {
        var message = OutboxMessage.Create(
            "payments.succeeded",
            new PaymentSucceededEvent(Guid.NewGuid(), Guid.NewGuid(), 25m),
            "QuickBite.Payments.Api");

        var retryAt = DateTimeOffset.UtcNow.AddSeconds(30);
        message.MarkFailed(new string('x', 1_100), retryAt);

        Assert.Equal(1, message.PublishAttempts);
        Assert.Equal(retryAt, message.NextAttemptAtUtc);
        Assert.Equal(1_000, message.LastError?.Length);
    }

    [Fact]
    public void InboxMessage_mark_processed_records_completion_time()
    {
        var processedAt = DateTimeOffset.UtcNow;
        var message = new InboxMessage(Guid.NewGuid(), "quickbite-payments", "orders.created", IntegrationEventTypes.OrderCreated);

        message.MarkProcessed(processedAt);

        Assert.Equal(processedAt, message.ProcessedAtUtc);
    }
}

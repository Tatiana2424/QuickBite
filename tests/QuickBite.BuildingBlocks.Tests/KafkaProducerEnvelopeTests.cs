using QuickBite.BuildingBlocks.Contracts;
using QuickBite.BuildingBlocks.Kafka;

namespace QuickBite.BuildingBlocks.Tests;

public sealed class KafkaProducerEnvelopeTests
{
    [Fact]
    public void CreateEnvelope_adds_required_operational_metadata()
    {
        var message = new PaymentSucceededEvent(Guid.NewGuid(), Guid.NewGuid(), 24.50m);

        var envelope = KafkaProducer.CreateEnvelope(message, "QuickBite.Payments.Api");

        Assert.NotEqual(Guid.Empty, envelope.EventId);
        Assert.NotEqual(Guid.Empty, envelope.CorrelationId);
        Assert.Null(envelope.CausationId);
        Assert.Equal(IntegrationEventTypes.PaymentSucceeded, envelope.EventType);
        Assert.Equal(1, envelope.EventVersion);
        Assert.Equal("QuickBite.Payments.Api", envelope.Producer);
        Assert.Equal(message, envelope.Payload);
        Assert.True(envelope.OccurredAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ResolveMessageKey_uses_order_id_when_available()
    {
        var orderId = Guid.NewGuid();
        var fallback = Guid.NewGuid();
        var message = new OrderCreatedEvent(orderId, Guid.NewGuid(), 12.99m, []);

        var key = KafkaProducer.ResolveMessageKey(message, fallback);

        Assert.Equal(orderId.ToString("N"), key);
    }

    [Fact]
    public void ResolveMessageKey_uses_fallback_when_order_id_is_not_available()
    {
        var fallback = Guid.NewGuid();
        var message = new DeliveryCompletedEvent(Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var key = KafkaProducer.ResolveMessageKey(message, fallback);

        Assert.Equal(fallback.ToString("N"), key);
    }
}

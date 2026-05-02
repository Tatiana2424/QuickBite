using System.Text.Json;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Contracts;

namespace QuickBite.QualityGates.Tests;

public sealed class WorkflowContractTests
{
    [Fact]
    public void Order_payment_delivery_events_preserve_order_key_and_correlation_chain()
    {
        var orderId = Guid.NewGuid();
        var orderCreated = new OrderCreatedEvent(
            orderId,
            Guid.NewGuid(),
            27.50m,
            new[] { new OrderCreatedItem(Guid.NewGuid(), "Chicken Bowl", 1, 27.50m) });

        var orderOutbox = OutboxMessage.Create("quickbite.orders.order-created.v1", orderCreated, "QuickBite.Orders.Api");

        var paymentSucceeded = new PaymentSucceededEvent(orderId, Guid.NewGuid(), orderCreated.TotalAmount);
        var paymentOutbox = OutboxMessage.Create(
            "quickbite.payments.payment-succeeded.v1",
            paymentSucceeded,
            "QuickBite.Payments.Api",
            orderOutbox.CorrelationId,
            orderOutbox.EventId);

        var deliveryAssigned = new DeliveryAssignedEvent(orderId, Guid.NewGuid(), Guid.NewGuid(), "Mia Brooks");
        var deliveryOutbox = OutboxMessage.Create(
            "quickbite.delivery.delivery-assigned.v1",
            deliveryAssigned,
            "QuickBite.Delivery.Api",
            paymentOutbox.CorrelationId,
            paymentOutbox.EventId);

        Assert.Equal(orderId.ToString("N"), orderOutbox.MessageKey);
        Assert.Equal(orderOutbox.MessageKey, paymentOutbox.MessageKey);
        Assert.Equal(orderOutbox.MessageKey, deliveryOutbox.MessageKey);
        Assert.Equal(orderOutbox.CorrelationId, paymentOutbox.CorrelationId);
        Assert.Equal(orderOutbox.CorrelationId, deliveryOutbox.CorrelationId);
        Assert.Equal(orderOutbox.EventId, paymentOutbox.CausationId);
        Assert.Equal(paymentOutbox.EventId, deliveryOutbox.CausationId);
        AssertEnvelope(orderOutbox.EnvelopeJson, "order.created", "QuickBite.Orders.Api");
        AssertEnvelope(paymentOutbox.EnvelopeJson, "payment.succeeded", "QuickBite.Payments.Api");
        AssertEnvelope(deliveryOutbox.EnvelopeJson, "delivery.assigned", "QuickBite.Delivery.Api");
    }

    [Fact]
    public void Payment_failure_event_is_available_for_order_compensation_flow()
    {
        var paymentFailed = new PaymentFailedEvent(Guid.NewGuid(), Guid.NewGuid(), 15m, "Payment provider declined the transaction.");
        var outbox = OutboxMessage.Create("quickbite.payments.payment-failed.v1", paymentFailed, "QuickBite.Payments.Api");

        Assert.Equal("payment.failed", outbox.EventType);
        Assert.Contains("payment provider declined", outbox.EnvelopeJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(paymentFailed.OrderId.ToString("N"), outbox.MessageKey);
    }

    private static void AssertEnvelope(string envelopeJson, string eventType, string producer)
    {
        using var envelope = JsonDocument.Parse(envelopeJson);
        Assert.Equal(eventType, envelope.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(1, envelope.RootElement.GetProperty("eventVersion").GetInt32());
        Assert.Equal(producer, envelope.RootElement.GetProperty("producer").GetString());
        Assert.True(envelope.RootElement.GetProperty("eventId").GetGuid() != Guid.Empty);
    }
}

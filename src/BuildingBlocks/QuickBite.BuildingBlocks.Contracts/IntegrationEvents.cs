namespace QuickBite.BuildingBlocks.Contracts;

public interface IIntegrationEvent
{
    string EventType { get; }
}

public sealed record EventEnvelope<T>(Guid EventId, DateTimeOffset OccurredAtUtc, string EventType, T Payload)
    where T : IIntegrationEvent;

public sealed record OrderCreatedEvent(Guid OrderId, Guid UserId, decimal TotalAmount, IReadOnlyCollection<OrderCreatedItem> Items) : IIntegrationEvent
{
    public string EventType => "order.created";
}

public sealed record OrderCreatedItem(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);

public sealed record PaymentSucceededEvent(Guid OrderId, Guid PaymentId, decimal Amount) : IIntegrationEvent
{
    public string EventType => "payment.succeeded";
}

public sealed record PaymentFailedEvent(Guid OrderId, Guid PaymentId, decimal Amount, string Reason) : IIntegrationEvent
{
    public string EventType => "payment.failed";
}

public sealed record DeliveryAssignedEvent(Guid OrderId, Guid DeliveryId, Guid CourierId, string CourierName) : IIntegrationEvent
{
    public string EventType => "delivery.assigned";
}

public sealed record DeliveryCompletedEvent(Guid OrderId, Guid DeliveryId, DateTimeOffset CompletedAtUtc) : IIntegrationEvent
{
    public string EventType => "delivery.completed";
}

namespace QuickBite.BuildingBlocks.Contracts;

public interface IIntegrationEvent
{
    string EventType { get; }
    int EventVersion => 1;
}

public sealed record EventEnvelope<T>(
    Guid EventId,
    string EventType,
    int EventVersion,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId,
    Guid? CausationId,
    string Producer,
    T Payload)
    where T : IIntegrationEvent;

public static class IntegrationEventTypes
{
    public const string OrderCreated = "order.created";
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed = "payment.failed";
    public const string DeliveryAssigned = "delivery.assigned";
    public const string DeliveryCompleted = "delivery.completed";
}

public sealed record OrderCreatedEvent(Guid OrderId, Guid UserId, decimal TotalAmount, IReadOnlyCollection<OrderCreatedItem> Items) : IIntegrationEvent
{
    public string EventType => IntegrationEventTypes.OrderCreated;
}

public sealed record OrderCreatedItem(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);

public sealed record PaymentSucceededEvent(Guid OrderId, Guid PaymentId, decimal Amount) : IIntegrationEvent
{
    public string EventType => IntegrationEventTypes.PaymentSucceeded;
}

public sealed record PaymentFailedEvent(Guid OrderId, Guid PaymentId, decimal Amount, string Reason) : IIntegrationEvent
{
    public string EventType => IntegrationEventTypes.PaymentFailed;
}

public sealed record DeliveryAssignedEvent(Guid OrderId, Guid DeliveryId, Guid CourierId, string CourierName) : IIntegrationEvent
{
    public string EventType => IntegrationEventTypes.DeliveryAssigned;
}

public sealed record DeliveryCompletedEvent(Guid OrderId, Guid DeliveryId, DateTimeOffset CompletedAtUtc) : IIntegrationEvent
{
    public string EventType => IntegrationEventTypes.DeliveryCompleted;
}

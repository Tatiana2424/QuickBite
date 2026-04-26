using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Delivery.Domain;

public enum DeliveryStatus
{
    Created = 0,
    Assigned = 1,
    Completed = 2
}

public sealed class Courier : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;

    private Courier()
    {
    }

    public Courier(string name, string phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
    }
}

public sealed class DeliveryStatusHistory : Entity
{
    public Guid DeliveryId { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public Delivery? Delivery { get; private set; }

    private DeliveryStatusHistory()
    {
    }

    public DeliveryStatusHistory(Guid deliveryId, DeliveryStatus status, string reason)
    {
        DeliveryId = deliveryId;
        Status = status;
        Reason = reason;
        ChangedAtUtc = DateTimeOffset.UtcNow;
    }
}

public sealed class Delivery : Entity
{
    public Guid OrderId { get; private set; }
    public Guid CourierId { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Assigned;
    public Courier? Courier { get; private set; }

    private Delivery()
    {
    }

    public Delivery(Guid orderId, Guid courierId)
    {
        OrderId = orderId;
        CourierId = courierId;
        Status = DeliveryStatus.Assigned;
    }
}

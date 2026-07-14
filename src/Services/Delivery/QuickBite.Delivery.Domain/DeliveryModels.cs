using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Delivery.Domain;

public enum DeliveryStatus
{
    Created = 0,
    Assigned = 1,
    Accepted = 2,
    PickedUp = 3,
    Delivered = 4,
    Completed = 5
}

public sealed class Courier : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;

    private Courier()
    {
    }

    public Courier(string name, string phoneNumber, Guid? id = null)
    {
        if (id.HasValue)
        {
            UseSeedIdentity(id.Value);
        }

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
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public Courier? Courier { get; private set; }

    private Delivery()
    {
    }

    public Delivery(
        Guid orderId,
        Guid courierId,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string postalCode,
        string country,
        Guid? id = null)
    {
        if (id.HasValue)
        {
            UseSeedIdentity(id.Value);
        }

        OrderId = orderId;
        CourierId = courierId;
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
        Status = DeliveryStatus.Assigned;
    }

    public void Accept()
    {
        if (Status is DeliveryStatus.Assigned)
        {
            Status = DeliveryStatus.Accepted;
            Touch();
        }
    }

    public void MarkPickedUp()
    {
        if (Status is DeliveryStatus.Accepted or DeliveryStatus.Assigned)
        {
            Status = DeliveryStatus.PickedUp;
            Touch();
        }
    }

    public void MarkDelivered()
    {
        if (Status is DeliveryStatus.PickedUp or DeliveryStatus.Accepted or DeliveryStatus.Assigned)
        {
            Status = DeliveryStatus.Delivered;
            Touch();
        }
    }
}

using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Orders.Domain;

public enum OrderStatus
{
    Pending = 0,
    PaymentProcessing = 1,
    Confirmed = 2,
    Failed = 3
}

public sealed class Order : Entity
{
    public Guid UserId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; private set; }
    public string DeliveryAddressLine1 { get; private set; } = string.Empty;
    public string? DeliveryAddressLine2 { get; private set; }
    public string DeliveryAddressCity { get; private set; } = string.Empty;
    public string DeliveryAddressState { get; private set; } = string.Empty;
    public string DeliveryAddressPostalCode { get; private set; } = string.Empty;
    public string DeliveryAddressCountry { get; private set; } = string.Empty;
    public List<OrderItem> Items { get; private set; } = new();

    private Order()
    {
    }

    public Order(
        Guid userId,
        IEnumerable<OrderItem> items,
        string deliveryAddressLine1,
        string? deliveryAddressLine2,
        string deliveryAddressCity,
        string deliveryAddressState,
        string deliveryAddressPostalCode,
        string deliveryAddressCountry,
        string? idempotencyKey = null)
    {
        UserId = userId;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        DeliveryAddressLine1 = deliveryAddressLine1.Trim();
        DeliveryAddressLine2 = string.IsNullOrWhiteSpace(deliveryAddressLine2) ? null : deliveryAddressLine2.Trim();
        DeliveryAddressCity = deliveryAddressCity.Trim();
        DeliveryAddressState = deliveryAddressState.Trim();
        DeliveryAddressPostalCode = deliveryAddressPostalCode.Trim();
        DeliveryAddressCountry = deliveryAddressCountry.Trim();
        Items = items.ToList();
        TotalAmount = Items.Sum(x => x.UnitPrice * x.Quantity);
        Status = OrderStatus.PaymentProcessing;
    }

    public void MarkConfirmed()
    {
        if (Status is OrderStatus.Confirmed)
        {
            return;
        }

        Status = OrderStatus.Confirmed;
        Touch();
    }

    public void MarkPaymentFailed()
    {
        if (Status is OrderStatus.Failed)
        {
            return;
        }

        Status = OrderStatus.Failed;
        Touch();
    }
}

public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Order? Order { get; private set; }

    private OrderItem()
    {
    }

    public OrderItem(Guid menuItemId, string name, int quantity, decimal unitPrice)
    {
        MenuItemId = menuItemId;
        Name = name;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

public sealed class OrderStatusHistory : Entity
{
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public Order? Order { get; private set; }

    private OrderStatusHistory()
    {
    }

    public OrderStatusHistory(Guid orderId, OrderStatus status, string reason)
    {
        OrderId = orderId;
        Status = status;
        Reason = reason;
        ChangedAtUtc = DateTimeOffset.UtcNow;
    }
}

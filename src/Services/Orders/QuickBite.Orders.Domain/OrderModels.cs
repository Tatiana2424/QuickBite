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
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();

    private Order()
    {
    }

    public Order(Guid userId, IEnumerable<OrderItem> items)
    {
        UserId = userId;
        Items = items.ToList();
        TotalAmount = Items.Sum(x => x.UnitPrice * x.Quantity);
        Status = OrderStatus.PaymentProcessing;
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

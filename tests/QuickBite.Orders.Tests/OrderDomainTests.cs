using QuickBite.Orders.Domain;

namespace QuickBite.Orders.Tests;

public sealed class OrderDomainTests
{
    [Fact]
    public void Order_calculates_total_and_records_idempotency_key()
    {
        var order = new Order(
            Guid.NewGuid(),
            [
                new OrderItem(Guid.NewGuid(), "Burger", 2, 12.50m),
                new OrderItem(Guid.NewGuid(), "Fries", 1, 4.25m)
            ],
            "checkout-123");

        Assert.Equal(OrderStatus.PaymentProcessing, order.Status);
        Assert.Equal(29.25m, order.TotalAmount);
        Assert.Equal("checkout-123", order.IdempotencyKey);
    }
}

using QuickBite.Orders.Domain;

namespace QuickBite.Orders.Tests;

public sealed class OrderSagaStateTests
{
    [Fact]
    public void New_order_starts_payment_processing_and_can_be_confirmed()
    {
        var order = new Order(
            Guid.NewGuid(),
            [new OrderItem(Guid.NewGuid(), "Burger", 1, 12.50m)],
            "123 Market Street",
            null,
            "Seattle",
            "WA",
            "98101",
            "USA",
            "order-key-1");

        order.MarkConfirmed();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void New_order_can_transition_to_failed_when_payment_fails()
    {
        var order = new Order(
            Guid.NewGuid(),
            [new OrderItem(Guid.NewGuid(), "Burger", 1, 12.50m)],
            "123 Market Street",
            null,
            "Seattle",
            "WA",
            "98101",
            "USA");

        order.MarkPaymentFailed();

        Assert.Equal(OrderStatus.Failed, order.Status);
    }
}

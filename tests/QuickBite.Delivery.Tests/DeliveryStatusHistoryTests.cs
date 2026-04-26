using QuickBite.Delivery.Domain;

namespace QuickBite.Delivery.Tests;

public sealed class DeliveryStatusHistoryTests
{
    [Fact]
    public void DeliveryStatusHistory_records_delivery_state_change()
    {
        var deliveryId = Guid.NewGuid();

        var history = new DeliveryStatusHistory(deliveryId, DeliveryStatus.Assigned, "Courier assigned");

        Assert.Equal(deliveryId, history.DeliveryId);
        Assert.Equal(DeliveryStatus.Assigned, history.Status);
        Assert.Equal("Courier assigned", history.Reason);
    }
}

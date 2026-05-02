using QuickBite.Delivery.Domain;

namespace QuickBite.Delivery.Tests;

public sealed class DeliveryDomainTests
{
    [Fact]
    public void Delivery_starts_assigned_to_selected_courier()
    {
        var courier = new Courier("Mia Brooks", "+1-555-0102");
        var delivery = new QuickBite.Delivery.Domain.Delivery(Guid.NewGuid(), courier.Id);

        Assert.Equal(courier.Id, delivery.CourierId);
        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
    }
}

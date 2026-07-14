using QuickBite.Delivery.Domain;

namespace QuickBite.Delivery.Tests;

public sealed class DeliveryDomainTests
{
    [Fact]
    public void Delivery_starts_assigned_to_selected_courier()
    {
        var courier = new Courier("Mia Brooks", "+1-555-0102");
        var delivery = new QuickBite.Delivery.Domain.Delivery(
            Guid.NewGuid(),
            courier.Id,
            "123 Market Street",
            null,
            "Seattle",
            "WA",
            "98101",
            "USA");

        Assert.Equal(courier.Id, delivery.CourierId);
        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Equal("123 Market Street", delivery.AddressLine1);
    }
}

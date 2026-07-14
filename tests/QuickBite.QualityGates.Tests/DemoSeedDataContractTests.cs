using QuickBite.BuildingBlocks.Common;

namespace QuickBite.QualityGates.Tests;

public sealed class DemoSeedDataContractTests
{
    [Fact]
    public void Demo_seed_data_has_stable_ids_for_cross_service_journeys()
    {
        Assert.NotEqual(Guid.Empty, DemoSeedData.CustomerUserId);
        Assert.NotEqual(Guid.Empty, DemoSeedData.UrbanBowlRestaurantId);
        Assert.NotEqual(Guid.Empty, DemoSeedData.UrbanBowlChickenPowerBowlId);
        Assert.NotEqual(Guid.Empty, DemoSeedData.DemoConfirmedOrderId);
        Assert.NotEqual(DemoSeedData.DemoConfirmedOrderId, DemoSeedData.DemoProcessingOrderId);
        Assert.NotEqual(DemoSeedData.DemoProcessingOrderId, DemoSeedData.DemoFailedOrderId);
        Assert.Equal("Pass123!", DemoSeedData.DemoPassword);
    }

    [Fact]
    public void Catalog_seed_keeps_home_page_populated()
    {
        var source = File.ReadAllText(SourcePath(
            "Services",
            "Catalog",
            "QuickBite.Catalog.Infrastructure",
            "CatalogInfrastructure.cs"));

        Assert.Contains("Breakfast Club", source, StringComparison.Ordinal);
        Assert.Contains("Curry House", source, StringComparison.Ordinal);
        Assert.True(CountOccurrences(source, "new MenuItem(") >= 18);
        Assert.True(CountOccurrences(source, "new Restaurant(") >= 6);
    }

    [Fact]
    public void Identity_seed_documents_customer_admin_and_courier_paths()
    {
        var source = File.ReadAllText(SourcePath(
            "Services",
            "Identity",
            "QuickBite.Identity.Infrastructure",
            "IdentityInfrastructure.cs"));

        Assert.Contains("customer@quickbite.local", source, StringComparison.Ordinal);
        Assert.Contains("family@quickbite.local", source, StringComparison.Ordinal);
        Assert.Contains("restaurant@quickbite.local", source, StringComparison.Ordinal);
        Assert.Contains("courier@quickbite.local", source, StringComparison.Ordinal);
        Assert.Contains("admin@quickbite.local", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Demo_orders_cover_payment_and_delivery_states()
    {
        var ordersSource = File.ReadAllText(SourcePath(
            "Services",
            "Orders",
            "QuickBite.Orders.Infrastructure",
            "OrdersInfrastructure.cs"));
        var paymentsSource = File.ReadAllText(SourcePath(
            "Services",
            "Payments",
            "QuickBite.Payments.Infrastructure",
            "PaymentsInfrastructure.cs"));
        var deliverySource = File.ReadAllText(SourcePath(
            "Services",
            "Delivery",
            "QuickBite.Delivery.Infrastructure",
            "DeliveryInfrastructure.cs"));

        Assert.Contains("DemoSeedData.DemoConfirmedOrderId", ordersSource, StringComparison.Ordinal);
        Assert.Contains("MarkConfirmed", ordersSource, StringComparison.Ordinal);
        Assert.Contains("DemoSeedData.DemoProcessingOrderId", ordersSource, StringComparison.Ordinal);
        Assert.Contains("DemoSeedData.DemoFailedOrderId", ordersSource, StringComparison.Ordinal);
        Assert.Contains("MarkPaymentFailed", ordersSource, StringComparison.Ordinal);
        Assert.Contains("PaymentStatus.Succeeded", paymentsSource, StringComparison.Ordinal);
        Assert.Contains("PaymentStatus.Failed", paymentsSource, StringComparison.Ordinal);
        Assert.Contains("DemoSeedData.DemoConfirmedOrderId", deliverySource, StringComparison.Ordinal);
    }

    private static string SourcePath(params string[] segments)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { root, "src" }.Concat(segments).ToArray());
    }

    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}

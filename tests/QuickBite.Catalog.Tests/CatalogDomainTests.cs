using QuickBite.Catalog.Domain;

namespace QuickBite.Catalog.Tests;

public sealed class CatalogDomainTests
{
    [Fact]
    public void Restaurant_keeps_catalog_identity_and_menu_items_can_attach_to_it()
    {
        var restaurant = new Restaurant("Urban Bowl", "Healthy", "Balanced bowls and fresh wraps.");
        var menuItem = new MenuItem(restaurant.Id, "Chicken Bowl", "Rice, greens, and grilled chicken.", 13.50m);

        restaurant.MenuItems.Add(menuItem);

        Assert.Equal("Urban Bowl", restaurant.Name);
        Assert.Equal("Healthy", restaurant.Cuisine);
        Assert.Single(restaurant.MenuItems);
        Assert.Equal(restaurant.Id, restaurant.MenuItems[0].RestaurantId);
        Assert.Equal(13.50m, restaurant.MenuItems[0].Price);
    }
}

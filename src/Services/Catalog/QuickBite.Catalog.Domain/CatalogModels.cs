using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Catalog.Domain;

public sealed class Restaurant : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Cuisine { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public List<MenuItem> MenuItems { get; private set; } = new();

    private Restaurant()
    {
    }

    public Restaurant(string name, string cuisine, string description)
    {
        Name = name;
        Cuisine = cuisine;
        Description = description;
    }
}

public sealed class MenuItem : Entity
{
    public Guid RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Restaurant? Restaurant { get; private set; }

    private MenuItem()
    {
    }

    public MenuItem(Guid restaurantId, string name, string description, decimal price)
    {
        RestaurantId = restaurantId;
        Name = name;
        Description = description;
        Price = price;
    }
}

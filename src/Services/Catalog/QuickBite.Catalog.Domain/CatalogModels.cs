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

    public Restaurant(string name, string cuisine, string description, Guid? id = null)
    {
        if (id.HasValue)
        {
            UseSeedIdentity(id.Value);
        }

        Update(name, cuisine, description);
    }

    public void Update(string name, string cuisine, string description)
    {
        Name = name.Trim();
        Cuisine = cuisine.Trim();
        Description = description.Trim();
        Touch();
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

    public MenuItem(Guid restaurantId, string name, string description, decimal price, Guid? id = null)
    {
        if (id.HasValue)
        {
            UseSeedIdentity(id.Value);
        }

        RestaurantId = restaurantId;
        Update(name, description, price);
    }

    public void Update(string name, string description, decimal price)
    {
        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        Touch();
    }
}

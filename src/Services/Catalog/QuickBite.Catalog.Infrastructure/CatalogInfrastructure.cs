using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickBite.BuildingBlocks.Common;
using QuickBite.Catalog.Application;
using QuickBite.Catalog.Domain;

namespace QuickBite.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("Restaurants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Cuisine).HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasMany(x => x.MenuItems).WithOne(x => x.Restaurant).HasForeignKey(x => x.RestaurantId);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("MenuItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Price).HasColumnType("decimal(10,2)");
        });
    }
}

public static class CatalogInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConfigurationGuard.GetRequiredConnectionString(configuration, "DefaultConnection");

        services.AddDatabaseInitializationOptions(configuration);
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<ICatalogService, CatalogService>();
        return services;
    }

    public static async Task EnsureCatalogDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await serviceProvider.InitializeDatabaseAsync<CatalogDbContext>(SeedAsync);
    }

    private static async Task SeedAsync(
        CatalogDbContext dbContext,
        DatabaseInitializationOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.SeedDemoData || await dbContext.Restaurants.AnyAsync(cancellationToken))
        {
            return;
        }

        var urbanBowl = new Restaurant("Urban Bowl", "Healthy", "Balanced bowls, protein plates, and fresh wraps.", DemoSeedData.UrbanBowlRestaurantId);
        var pizzaPort = new Restaurant("Pizza Port", "Italian", "Stone-baked pizzas, salads, and late-night sides.", DemoSeedData.PizzaPortRestaurantId);
        var tacoLane = new Restaurant("Taco Lane", "Mexican", "Street tacos, burrito bowls, aguas frescas, and bright salsas.", DemoSeedData.TacoLaneRestaurantId);
        var sushiCentral = new Restaurant("Sushi Central", "Japanese", "Sushi sets, rice bowls, miso soup, and crisp sides.", DemoSeedData.SushiCentralRestaurantId);
        var breakfastClub = new Restaurant("Breakfast Club", "Breakfast", "Morning sandwiches, pancakes, coffee, and brunch favorites.", DemoSeedData.BreakfastClubRestaurantId);
        var curryHouse = new Restaurant("Curry House", "Indian", "Comforting curries, biryani, naan, and vegetarian plates.", DemoSeedData.CurryHouseRestaurantId);

        dbContext.Restaurants.AddRange(urbanBowl, pizzaPort, tacoLane, sushiCentral, breakfastClub, curryHouse);
        dbContext.MenuItems.AddRange(
            new MenuItem(urbanBowl.Id, "Chicken Power Bowl", "Grilled chicken, rice, greens, sesame dressing.", 12.90m, DemoSeedData.UrbanBowlChickenPowerBowlId),
            new MenuItem(urbanBowl.Id, "Falafel Wrap", "Falafel, hummus, slaw, and pickled onions.", 9.50m, DemoSeedData.UrbanBowlFalafelWrapId),
            new MenuItem(urbanBowl.Id, "Green Detox Smoothie", "Spinach, mango, banana, ginger.", 5.90m),
            new MenuItem(urbanBowl.Id, "Salmon Grain Bowl", "Roasted salmon, quinoa, avocado, cucumber, and herbs.", 15.40m),
            new MenuItem(pizzaPort.Id, "Margherita", "San Marzano tomato, mozzarella, basil.", 11.00m, DemoSeedData.PizzaPortMargheritaId),
            new MenuItem(pizzaPort.Id, "Pepperoni Feast", "Pepperoni, mozzarella, oregano.", 13.50m),
            new MenuItem(pizzaPort.Id, "Garlic Knots", "Soft dough knots with garlic butter.", 4.80m),
            new MenuItem(pizzaPort.Id, "Arugula Caesar", "Romaine, arugula, shaved parmesan, lemon Caesar dressing.", 8.75m),
            new MenuItem(tacoLane.Id, "Birria Tacos", "Three crisp tacos with slow-cooked beef and consomme.", 14.25m, DemoSeedData.TacoLaneBirriaTacosId),
            new MenuItem(tacoLane.Id, "Veggie Burrito Bowl", "Rice, beans, peppers, corn salsa, guacamole, and crema.", 10.80m),
            new MenuItem(tacoLane.Id, "Chips and Salsa Flight", "House chips with verde, roja, and mango salsa.", 5.25m),
            new MenuItem(sushiCentral.Id, "Salmon Sushi Set", "Six salmon nigiri, spicy tuna roll, and miso soup.", 18.90m, DemoSeedData.SushiCentralSalmonSetId),
            new MenuItem(sushiCentral.Id, "Chicken Katsu Don", "Crispy chicken, rice, cabbage, pickles, and tonkatsu sauce.", 13.20m),
            new MenuItem(sushiCentral.Id, "Edamame", "Steamed soybeans with sea salt.", 4.50m),
            new MenuItem(breakfastClub.Id, "Bacon Egg Croissant", "Buttery croissant with egg, bacon, cheddar, and aioli.", 8.95m),
            new MenuItem(breakfastClub.Id, "Blueberry Pancakes", "Three pancakes with blueberries, maple butter, and syrup.", 11.25m),
            new MenuItem(breakfastClub.Id, "Cold Brew", "Slow-steeped coffee over ice.", 4.25m),
            new MenuItem(curryHouse.Id, "Butter Chicken", "Tomato cream curry with basmati rice.", 14.90m),
            new MenuItem(curryHouse.Id, "Chana Masala", "Chickpeas simmered with tomato, ginger, and spices.", 11.75m),
            new MenuItem(curryHouse.Id, "Garlic Naan", "Tandoor bread brushed with garlic butter.", 3.95m));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class CatalogService(CatalogDbContext dbContext) : ICatalogService
{
    public async Task<IReadOnlyCollection<RestaurantSummaryDto>> GetRestaurantsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Restaurants
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RestaurantSummaryDto(x.Id, x.Name, x.Cuisine, x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<RestaurantDetailsDto?> GetRestaurantAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Restaurants
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new RestaurantDetailsDto(
                x.Id,
                x.Name,
                x.Cuisine,
                x.Description,
                x.MenuItems
                    .OrderBy(item => item.Name)
                    .Select(item => new MenuItemDto(item.Id, item.RestaurantId, item.Name, item.Description, item.Price))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MenuItemDto>> GetMenuAsync(Guid restaurantId, CancellationToken cancellationToken)
    {
        return await dbContext.MenuItems
            .AsNoTracking()
            .Where(x => x.RestaurantId == restaurantId)
            .OrderBy(x => x.Name)
            .Select(x => new MenuItemDto(x.Id, x.RestaurantId, x.Name, x.Description, x.Price))
            .ToListAsync(cancellationToken);
    }

    public async Task<RestaurantDetailsDto> CreateRestaurantAsync(RestaurantMutationRequest request, CancellationToken cancellationToken)
    {
        var restaurant = new Restaurant(request.Name, request.Cuisine, request.Description);
        dbContext.Restaurants.Add(restaurant);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RestaurantDetailsDto(restaurant.Id, restaurant.Name, restaurant.Cuisine, restaurant.Description, []);
    }

    public async Task<RestaurantDetailsDto?> UpdateRestaurantAsync(Guid id, RestaurantMutationRequest request, CancellationToken cancellationToken)
    {
        var restaurant = await dbContext.Restaurants.Include(x => x.MenuItems).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (restaurant is null)
        {
            return null;
        }

        restaurant.Update(request.Name, request.Cuisine, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RestaurantDetailsDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.Cuisine,
            restaurant.Description,
            restaurant.MenuItems.OrderBy(x => x.Name).Select(MapMenuItem).ToList());
    }

    public async Task<MenuItemDto?> CreateMenuItemAsync(Guid restaurantId, MenuItemMutationRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Restaurants.AnyAsync(x => x.Id == restaurantId, cancellationToken))
        {
            return null;
        }

        var menuItem = new MenuItem(restaurantId, request.Name, request.Description, request.Price);
        dbContext.MenuItems.Add(menuItem);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapMenuItem(menuItem);
    }

    public async Task<MenuItemDto?> UpdateMenuItemAsync(Guid restaurantId, Guid menuItemId, MenuItemMutationRequest request, CancellationToken cancellationToken)
    {
        var menuItem = await dbContext.MenuItems.FirstOrDefaultAsync(
            x => x.RestaurantId == restaurantId && x.Id == menuItemId,
            cancellationToken);

        if (menuItem is null)
        {
            return null;
        }

        menuItem.Update(request.Name, request.Description, request.Price);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapMenuItem(menuItem);
    }

    private static MenuItemDto MapMenuItem(MenuItem item) => new(item.Id, item.RestaurantId, item.Name, item.Description, item.Price);
}

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseSqlServer(DesignTimeSqlServer.ResolveConnectionString("QuickBiteCatalogDb"));
        return new CatalogDbContext(optionsBuilder.Options);
    }
}

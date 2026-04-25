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

        var urbanBowl = new Restaurant("Urban Bowl", "Healthy", "Balanced bowls and fresh wraps.");
        var pizzaPort = new Restaurant("Pizza Port", "Italian", "Stone baked pizzas and sides.");

        dbContext.Restaurants.AddRange(urbanBowl, pizzaPort);
        dbContext.MenuItems.AddRange(
            new MenuItem(urbanBowl.Id, "Chicken Power Bowl", "Grilled chicken, rice, greens, sesame dressing.", 12.90m),
            new MenuItem(urbanBowl.Id, "Falafel Wrap", "Falafel, hummus, slaw, and pickled onions.", 9.50m),
            new MenuItem(urbanBowl.Id, "Green Detox Smoothie", "Spinach, mango, banana, ginger.", 5.90m),
            new MenuItem(pizzaPort.Id, "Margherita", "San Marzano tomato, mozzarella, basil.", 11.00m),
            new MenuItem(pizzaPort.Id, "Pepperoni Feast", "Pepperoni, mozzarella, oregano.", 13.50m),
            new MenuItem(pizzaPort.Id, "Garlic Knots", "Soft dough knots with garlic butter.", 4.80m));

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

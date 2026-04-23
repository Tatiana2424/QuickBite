using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Contracts;
using QuickBite.BuildingBlocks.Kafka;
using QuickBite.Orders.Application;
using QuickBite.Orders.Domain;

namespace QuickBite.Orders.Infrastructure;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(10,2)");
            entity.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(10,2)");
        });
    }
}

public static class OrdersInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConfigurationGuard.GetRequiredConnectionString(configuration, "DefaultConnection");

        services.AddOptions<DatabaseInitializationOptions>()
            .Bind(configuration.GetSection("DatabaseInitialization"));
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddKafkaInfrastructure(configuration);
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }

    public static async Task EnsureOrdersDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await serviceProvider.InitializeDatabaseAsync<OrdersDbContext>();
    }
}

internal sealed class OrderService(
    OrdersDbContext dbContext,
    IKafkaProducer kafkaProducer,
    IOptions<KafkaOptions> kafkaOptions) : IOrderService
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var items = request.Items.Select(item => new OrderItem(item.MenuItemId, item.Name, item.Quantity, item.UnitPrice)).ToList();
        var order = new Order(request.UserId, items);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var integrationEvent = new OrderCreatedEvent(
            order.Id,
            order.UserId,
            order.TotalAmount,
            order.Items.Select(x => new OrderCreatedItem(x.MenuItemId, x.Name, x.Quantity, x.UnitPrice)).ToList());

        await kafkaProducer.PublishAsync(kafkaOptions.Value.Topics.OrderCreated, integrationEvent, cancellationToken);
        return Map(order);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return order is null ? null : Map(order);
    }

    private static OrderDto Map(Order order) => new(
        order.Id,
        order.UserId,
        order.Status.ToString(),
        order.TotalAmount,
        order.Items.Select(x => new OrderItemDto(x.MenuItemId, x.Name, x.Quantity, x.UnitPrice)).ToList());
}

public sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer(DesignTimeSqlServer.ResolveConnectionString("QuickBiteOrdersDb"));
        return new OrdersDbContext(optionsBuilder.Options);
    }
}

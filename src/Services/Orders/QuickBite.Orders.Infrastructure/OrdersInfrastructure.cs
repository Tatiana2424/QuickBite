using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(x => x.DeliveryAddressLine1).HasMaxLength(200);
            entity.Property(x => x.DeliveryAddressLine2).HasMaxLength(200);
            entity.Property(x => x.DeliveryAddressCity).HasMaxLength(120);
            entity.Property(x => x.DeliveryAddressState).HasMaxLength(80);
            entity.Property(x => x.DeliveryAddressPostalCode).HasMaxLength(20);
            entity.Property(x => x.DeliveryAddressCountry).HasMaxLength(80);
            entity.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(300);
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId);
        });

        modelBuilder.ConfigureOutbox();
        modelBuilder.ConfigureInbox();
    }
}

public static class OrdersInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConfigurationGuard.GetRequiredConnectionString(configuration, "DefaultConnection");

        services.AddDatabaseInitializationOptions(configuration);
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddKafkaInfrastructure(configuration);
        services.AddScoped<IOrderService, OrderService>();
        services.AddHostedService<OrdersOutboxPublisher>();
        services.AddHostedService<PaymentSucceededConsumer>();
        services.AddHostedService<PaymentFailedConsumer>();
        return services;
    }

    public static async Task EnsureOrdersDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await serviceProvider.InitializeDatabaseAsync<OrdersDbContext>(SeedAsync);
    }

    private static async Task SeedAsync(
        OrdersDbContext dbContext,
        DatabaseInitializationOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.SeedDemoData || await dbContext.Orders.AnyAsync(cancellationToken))
        {
            return;
        }

        var confirmedOrder = new Order(
            DemoSeedData.CustomerUserId,
            [
                new OrderItem(DemoSeedData.UrbanBowlChickenPowerBowlId, "Chicken Power Bowl", 1, 12.90m),
                new OrderItem(DemoSeedData.UrbanBowlFalafelWrapId, "Falafel Wrap", 2, 9.50m)
            ],
            "123 Market Street",
            "Apt 4B",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "demo-confirmed-order",
            DemoSeedData.DemoConfirmedOrderId,
            DateTimeOffset.UtcNow.AddHours(-5));
        confirmedOrder.MarkConfirmed();

        var processingOrder = new Order(
            DemoSeedData.CustomerUserId,
            [new OrderItem(DemoSeedData.PizzaPortMargheritaId, "Margherita", 1, 11.00m)],
            "123 Market Street",
            "Apt 4B",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "demo-processing-order",
            DemoSeedData.DemoProcessingOrderId,
            DateTimeOffset.UtcNow.AddMinutes(-35));

        var failedOrder = new Order(
            DemoSeedData.CustomerUserId,
            [
                new OrderItem(DemoSeedData.SushiCentralSalmonSetId, "Salmon Sushi Set", 8, 18.90m),
                new OrderItem(DemoSeedData.TacoLaneBirriaTacosId, "Birria Tacos", 5, 14.25m)
            ],
            "123 Market Street",
            "Apt 4B",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "demo-failed-order",
            DemoSeedData.DemoFailedOrderId,
            DateTimeOffset.UtcNow.AddDays(-1));
        failedOrder.MarkPaymentFailed();

        dbContext.Orders.AddRange(confirmedOrder, processingOrder, failedOrder);
        dbContext.OrderStatusHistory.AddRange(
            new OrderStatusHistory(confirmedOrder.Id, OrderStatus.PaymentProcessing, "Demo order created."),
            new OrderStatusHistory(confirmedOrder.Id, OrderStatus.Confirmed, "Demo payment succeeded."),
            new OrderStatusHistory(processingOrder.Id, OrderStatus.PaymentProcessing, "Demo order is waiting for payment processing."),
            new OrderStatusHistory(failedOrder.Id, OrderStatus.PaymentProcessing, "Demo order created."),
            new OrderStatusHistory(failedOrder.Id, OrderStatus.Failed, "Demo payment failed."));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class OrderService(
    OrdersDbContext dbContext,
    IOptions<KafkaOptions> kafkaOptions) : IOrderService
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await dbContext.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x => x.UserId == request.UserId && x.IdempotencyKey == request.IdempotencyKey.Trim(),
                    cancellationToken);

            if (existing is not null)
            {
                return Map(existing);
            }
        }

        var items = request.Items.Select(item => new OrderItem(item.MenuItemId, item.Name, item.Quantity, item.UnitPrice)).ToList();
        var order = new Order(
            request.UserId,
            items,
            request.DeliveryAddress.Line1,
            request.DeliveryAddress.Line2,
            request.DeliveryAddress.City,
            request.DeliveryAddress.State,
            request.DeliveryAddress.PostalCode,
            request.DeliveryAddress.Country,
            request.IdempotencyKey);

        dbContext.Orders.Add(order);
        dbContext.OrderStatusHistory.Add(new OrderStatusHistory(order.Id, order.Status, "Order created and payment processing started."));

        var integrationEvent = new OrderCreatedEvent(
            order.Id,
            order.UserId,
            order.TotalAmount,
            MapAddressPayload(order),
            order.Items.Select(x => new OrderCreatedItem(x.MenuItemId, x.Name, x.Quantity, x.UnitPrice)).ToList());

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            kafkaOptions.Value.Topics.OrderCreated,
            integrationEvent,
            kafkaOptions.Value.Producer));

        await dbContext.SaveChangesAsync(cancellationToken);
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

    public async Task<OrderDto?> GetForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        return order is null ? null : Map(order);
    }

    public async Task<IReadOnlyCollection<OrderDto>> ListForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);
        var orders = await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return orders.Select(Map).ToList();
    }

    public async Task<OrderDetailsDto?> GetDetailsForUserByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        return order is null ? null : MapDetails(order);
    }

    public async Task<OrderSummaryPageDto> ListSummariesForUserAsync(
        Guid userId,
        int limit,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);
        var query = dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.UserId == userId);

        if (TryParseCursor(cursor, out var cursorCreatedAtUtc))
        {
            query = query.Where(x => x.CreatedAtUtc < cursorCreatedAtUtc);
        }

        var orders = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(safeLimit + 1)
            .ToListAsync(cancellationToken);

        var pageOrders = orders.Take(safeLimit).ToList();
        var nextCursor = orders.Count > safeLimit
            ? pageOrders[^1].CreatedAtUtc.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;

        return new OrderSummaryPageDto(pageOrders.Select(MapSummary).ToList(), nextCursor);
    }

    private static OrderDto Map(Order order) => new(
        order.Id,
        order.UserId,
        order.Status.ToString(),
        order.TotalAmount,
        order.CreatedAtUtc,
        MapAddress(order),
        order.Items.Select(x => new OrderItemDto(x.MenuItemId, x.Name, x.Quantity, x.UnitPrice)).ToList());

    private static OrderDetailsDto MapDetails(Order order) => new(
        order.Id,
        order.UserId,
        order.Status.ToString(),
        order.TotalAmount,
        order.CreatedAtUtc,
        MapAddress(order),
        order.Items.Select(x => new OrderItemDto(x.MenuItemId, x.Name, x.Quantity, x.UnitPrice)).ToList());

    private static DeliveryAddressDto MapAddress(Order order) => new(
        order.DeliveryAddressLine1,
        order.DeliveryAddressLine2,
        order.DeliveryAddressCity,
        order.DeliveryAddressState,
        order.DeliveryAddressPostalCode,
        order.DeliveryAddressCountry);

    private static DeliveryAddressPayload MapAddressPayload(Order order) => new(
        order.DeliveryAddressLine1,
        order.DeliveryAddressLine2,
        order.DeliveryAddressCity,
        order.DeliveryAddressState,
        order.DeliveryAddressPostalCode,
        order.DeliveryAddressCountry);

    private static OrderSummaryDto MapSummary(Order order)
    {
        var visibleItems = order.Items.Take(3).Select(x => $"{x.Quantity} x {x.Name}").ToList();
        var remainingCount = order.Items.Count - visibleItems.Count;
        var itemSummary = remainingCount > 0
            ? $"{string.Join(", ", visibleItems)} + {remainingCount} more"
            : string.Join(", ", visibleItems);

        return new OrderSummaryDto(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAtUtc,
            order.Items.Sum(x => x.Quantity),
            itemSummary);
    }

    private static bool TryParseCursor(string? cursor, out DateTimeOffset createdAtUtc)
    {
        createdAtUtc = default;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        return long.TryParse(cursor, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var milliseconds)
            && TrySetDateTimeOffset(milliseconds, out createdAtUtc);
    }

    private static bool TrySetDateTimeOffset(long milliseconds, out DateTimeOffset value)
    {
        try
        {
            value = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = default;
            return false;
        }
    }
}

internal sealed class OrdersOutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<OrdersOutboxPublisher> logger) : OutboxPublisherBackgroundService<OrdersDbContext>(scopeFactory, logger);

internal sealed class PaymentSucceededConsumer : KafkaConsumerBackgroundService<PaymentSucceededEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _topicName;

    public PaymentSucceededConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<PaymentSucceededConsumer> logger) : base(options, logger)
    {
        _scopeFactory = scopeFactory;
        _topicName = options.Value.Topics.PaymentSucceeded;
    }

    protected override string TopicName => _topicName;
    protected override string GroupId => "quickbite-orders-payment-succeeded";

    protected override async Task HandleAsync(EventEnvelope<PaymentSucceededEvent> envelope, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        if (await dbContext.InboxMessages.AnyAsync(x => x.EventId == envelope.EventId && x.Consumer == GroupId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var inboxMessage = new InboxMessage(envelope.EventId, GroupId, TopicName, envelope.EventType);
        dbContext.InboxMessages.Add(inboxMessage);

        var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == envelope.Payload.OrderId, cancellationToken);
        if (order is not null)
        {
            order.MarkConfirmed();
            dbContext.OrderStatusHistory.Add(new OrderStatusHistory(order.Id, order.Status, "Payment succeeded."));
        }

        inboxMessage.MarkProcessed(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

internal sealed class PaymentFailedConsumer : KafkaConsumerBackgroundService<PaymentFailedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _topicName;

    public PaymentFailedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<PaymentFailedConsumer> logger) : base(options, logger)
    {
        _scopeFactory = scopeFactory;
        _topicName = options.Value.Topics.PaymentFailed;
    }

    protected override string TopicName => _topicName;
    protected override string GroupId => "quickbite-orders-payment-failed";

    protected override async Task HandleAsync(EventEnvelope<PaymentFailedEvent> envelope, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        if (await dbContext.InboxMessages.AnyAsync(x => x.EventId == envelope.EventId && x.Consumer == GroupId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var inboxMessage = new InboxMessage(envelope.EventId, GroupId, TopicName, envelope.EventType);
        dbContext.InboxMessages.Add(inboxMessage);

        var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == envelope.Payload.OrderId, cancellationToken);
        if (order is not null)
        {
            order.MarkPaymentFailed();
            dbContext.OrderStatusHistory.Add(new OrderStatusHistory(order.Id, order.Status, envelope.Payload.Reason));
        }

        inboxMessage.MarkProcessed(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
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

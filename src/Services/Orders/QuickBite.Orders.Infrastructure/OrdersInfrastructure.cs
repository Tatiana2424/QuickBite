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
        await serviceProvider.InitializeDatabaseAsync<OrdersDbContext>();
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
        var order = new Order(request.UserId, items, request.IdempotencyKey);

        dbContext.Orders.Add(order);
        dbContext.OrderStatusHistory.Add(new OrderStatusHistory(order.Id, order.Status, "Order created and payment processing started."));

        var integrationEvent = new OrderCreatedEvent(
            order.Id,
            order.UserId,
            order.TotalAmount,
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

    private static OrderDto Map(Order order) => new(
        order.Id,
        order.UserId,
        order.Status.ToString(),
        order.TotalAmount,
        order.Items.Select(x => new OrderItemDto(x.MenuItemId, x.Name, x.Quantity, x.UnitPrice)).ToList());
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

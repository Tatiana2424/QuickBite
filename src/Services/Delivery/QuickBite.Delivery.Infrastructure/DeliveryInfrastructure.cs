using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Contracts;
using QuickBite.BuildingBlocks.Kafka;
using QuickBite.Delivery.Application;
using QuickBite.Delivery.Domain;

namespace QuickBite.Delivery.Infrastructure;

public sealed class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
{
    public DbSet<QuickBite.Delivery.Domain.Delivery> Deliveries => Set<QuickBite.Delivery.Domain.Delivery>();
    public DbSet<Courier> Couriers => Set<Courier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Courier>(entity =>
        {
            entity.ToTable("Couriers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.PhoneNumber).HasMaxLength(30);
        });

        modelBuilder.Entity<QuickBite.Delivery.Domain.Delivery>(entity =>
        {
            entity.ToTable("Deliveries");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.HasOne(x => x.Courier).WithMany().HasForeignKey(x => x.CourierId);
        });
    }
}

public static class DeliveryInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DeliveryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddKafkaInfrastructure(configuration);
        services.AddScoped<IDeliveryReadService, DeliveryReadService>();
        services.AddHostedService<PaymentSucceededConsumer>();
        return services;
    }

    public static async Task EnsureDeliveryDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        if (!await dbContext.Couriers.AnyAsync())
        {
            dbContext.Couriers.AddRange(
                new Courier("Alex Rider", "+1-555-0101"),
                new Courier("Mia Brooks", "+1-555-0102"));

            await dbContext.SaveChangesAsync();
        }
    }
}

internal sealed class DeliveryReadService(DeliveryDbContext dbContext) : IDeliveryReadService
{
    public async Task<DeliveryDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await dbContext.Deliveries
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .Select(x => new DeliveryDto(x.Id, x.OrderId, x.Status.ToString(), x.CourierId, x.Courier!.Name, x.Courier.PhoneNumber))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

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
    protected override string GroupId => "quickbite-delivery";

    protected override async Task HandleAsync(EventEnvelope<PaymentSucceededEvent> envelope, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();
        var kafkaOptions = scope.ServiceProvider.GetRequiredService<IOptions<KafkaOptions>>();

        if (await dbContext.Deliveries.AnyAsync(x => x.OrderId == envelope.Payload.OrderId, cancellationToken))
        {
            return;
        }

        var courier = await dbContext.Couriers.OrderBy(x => x.Name).FirstAsync(cancellationToken);
        var delivery = new QuickBite.Delivery.Domain.Delivery(envelope.Payload.OrderId, courier.Id);
        dbContext.Deliveries.Add(delivery);
        await dbContext.SaveChangesAsync(cancellationToken);

        await producer.PublishAsync(
            kafkaOptions.Value.Topics.DeliveryAssigned,
            new DeliveryAssignedEvent(delivery.OrderId, delivery.Id, courier.Id, courier.Name),
            cancellationToken);
    }
}

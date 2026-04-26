using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Contracts;
using QuickBite.BuildingBlocks.Kafka;
using QuickBite.Payments.Application;
using QuickBite.Payments.Domain;

namespace QuickBite.Payments.Infrastructure;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentStatusHistory> PaymentStatusHistory => Set<PaymentStatusHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            entity.Property(x => x.FailureReason).HasMaxLength(300);
        });

        modelBuilder.Entity<PaymentStatusHistory>(entity =>
        {
            entity.ToTable("PaymentStatusHistory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(300);
            entity.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId);
        });

        modelBuilder.ConfigureOutbox();
        modelBuilder.ConfigureInbox();
    }
}

public static class PaymentsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConfigurationGuard.GetRequiredConnectionString(configuration, "DefaultConnection");

        services.AddDatabaseInitializationOptions(configuration);
        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddKafkaInfrastructure(configuration);
        services.AddScoped<IPaymentReadService, PaymentReadService>();
        services.AddHostedService<OrderCreatedConsumer>();
        services.AddHostedService<PaymentsOutboxPublisher>();
        return services;
    }

    public static async Task EnsurePaymentsDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await serviceProvider.InitializeDatabaseAsync<PaymentsDbContext>();
    }
}

internal sealed class PaymentReadService(PaymentsDbContext dbContext) : IPaymentReadService
{
    public async Task<PaymentDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        return payment is null ? null : new PaymentDto(payment.Id, payment.OrderId, payment.Amount, payment.Status.ToString(), payment.FailureReason);
    }
}

internal sealed class PaymentsOutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentsOutboxPublisher> logger) : OutboxPublisherBackgroundService<PaymentsDbContext>(scopeFactory, logger);

internal sealed class OrderCreatedConsumer : KafkaConsumerBackgroundService<OrderCreatedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _topicName;

    public OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<OrderCreatedConsumer> logger) : base(options, logger)
    {
        _scopeFactory = scopeFactory;
        _topicName = options.Value.Topics.OrderCreated;
    }

    protected override string TopicName => _topicName;
    protected override string GroupId => "quickbite-payments";

    protected override async Task HandleAsync(EventEnvelope<OrderCreatedEvent> envelope, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var kafkaOptions = scope.ServiceProvider.GetRequiredService<IOptions<KafkaOptions>>();

        if (await dbContext.InboxMessages.AnyAsync(x => x.EventId == envelope.EventId && x.Consumer == GroupId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var inboxMessage = new InboxMessage(envelope.EventId, GroupId, TopicName, envelope.EventType);
        dbContext.InboxMessages.Add(inboxMessage);

        if (await dbContext.Payments.AnyAsync(x => x.OrderId == envelope.Payload.OrderId, cancellationToken))
        {
            inboxMessage.MarkProcessed(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var isSuccessful = envelope.Payload.TotalAmount <= 200m || envelope.Payload.Items.Count <= 4;
        var payment = new Payment(
            envelope.Payload.OrderId,
            envelope.Payload.TotalAmount,
            isSuccessful ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            isSuccessful ? null : "Payment was rejected by the simulated provider.");

        dbContext.Payments.Add(payment);
        dbContext.PaymentStatusHistory.Add(new PaymentStatusHistory(
            payment.Id,
            payment.Status,
            isSuccessful ? "Simulated payment provider approved the payment." : payment.FailureReason ?? "Payment failed."));

        if (payment.Status == PaymentStatus.Succeeded)
        {
            dbContext.OutboxMessages.Add(OutboxMessage.Create(
                kafkaOptions.Value.Topics.PaymentSucceeded,
                new PaymentSucceededEvent(payment.OrderId, payment.Id, payment.Amount),
                kafkaOptions.Value.Producer,
                envelope.CorrelationId,
                envelope.EventId));
        }
        else
        {
            dbContext.OutboxMessages.Add(OutboxMessage.Create(
                kafkaOptions.Value.Topics.PaymentFailed,
                new PaymentFailedEvent(payment.OrderId, payment.Id, payment.Amount, payment.FailureReason ?? "Payment failed."),
                kafkaOptions.Value.Producer,
                envelope.CorrelationId,
                envelope.EventId));
        }

        inboxMessage.MarkProcessed(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
        optionsBuilder.UseSqlServer(DesignTimeSqlServer.ResolveConnectionString("QuickBitePaymentsDb"));
        return new PaymentsDbContext(optionsBuilder.Options);
    }
}

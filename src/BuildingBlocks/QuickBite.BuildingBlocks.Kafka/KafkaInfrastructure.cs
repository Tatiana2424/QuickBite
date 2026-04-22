using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Contracts;

namespace QuickBite.BuildingBlocks.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public KafkaTopics Topics { get; set; } = new();
}

public sealed class KafkaTopics
{
    public string OrderCreated { get; set; } = "quickbite.order.created";
    public string PaymentSucceeded { get; set; } = "quickbite.payment.succeeded";
    public string PaymentFailed { get; set; } = "quickbite.payment.failed";
    public string DeliveryAssigned { get; set; } = "quickbite.delivery.assigned";
    public string DeliveryCompleted { get; set; } = "quickbite.delivery.completed";
}

public sealed class KafkaOptionsValidator : IValidateOptions<KafkaOptions>
{
    public ValidateOptionsResult Validate(string? name, KafkaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            return ValidateOptionsResult.Fail("Kafka:BootstrapServers must be configured.");
        }

        var topics = options.Topics;
        var missingTopics = new Dictionary<string, string?>
        {
            [nameof(topics.OrderCreated)] = topics.OrderCreated,
            [nameof(topics.PaymentSucceeded)] = topics.PaymentSucceeded,
            [nameof(topics.PaymentFailed)] = topics.PaymentFailed,
            [nameof(topics.DeliveryAssigned)] = topics.DeliveryAssigned,
            [nameof(topics.DeliveryCompleted)] = topics.DeliveryCompleted
        }
        .Where(pair => string.IsNullOrWhiteSpace(pair.Value))
        .Select(pair => pair.Key)
        .ToArray();

        return missingTopics.Length > 0
            ? ValidateOptionsResult.Fail($"Kafka topic names must be configured. Missing: {string.Join(", ", missingTopics)}.")
            : ValidateOptionsResult.Success;
    }
}

public interface IKafkaProducer
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent;
}

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options)
    {
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All
        }).Build();
    }

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        var envelope = new EventEnvelope<T>(Guid.NewGuid(), DateTimeOffset.UtcNow, message.EventType, message);
        var payload = JsonSerializer.Serialize(envelope, SerializerOptions);

        return _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = envelope.EventId.ToString("N"),
            Value = payload
        }, cancellationToken);
    }

    public void Dispose() => _producer.Dispose();
}

public abstract class KafkaConsumerBackgroundService<T> : BackgroundService where T : IIntegrationEvent
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly KafkaOptions _options;
    private readonly ILogger _logger;

    protected KafkaConsumerBackgroundService(IOptions<KafkaOptions> options, ILogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected abstract string TopicName { get; }
    protected abstract string GroupId { get; }
    protected abstract Task HandleAsync(EventEnvelope<T> envelope, CancellationToken cancellationToken);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(TopicName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null)
                    {
                        continue;
                    }

                    var envelope = JsonSerializer.Deserialize<EventEnvelope<T>>(result.Message.Value, SerializerOptions);
                    if (envelope is null)
                    {
                        _logger.LogWarning("Kafka message on topic {TopicName} could not be deserialized.", TopicName);
                        continue;
                    }

                    await HandleAsync(envelope, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException exception)
                {
                    _logger.LogError(exception, "Kafka consume error on topic {TopicName}.", TopicName);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Unexpected Kafka consumer failure on topic {TopicName}.", TopicName);
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }, stoppingToken);
    }
}

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<KafkaOptions>, KafkaOptionsValidator>();
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection("Kafka"))
            .ValidateOnStart();
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        return services;
    }
}

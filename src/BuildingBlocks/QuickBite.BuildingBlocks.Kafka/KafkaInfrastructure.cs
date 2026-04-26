using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBite.BuildingBlocks.Common;
using QuickBite.BuildingBlocks.Contracts;

namespace QuickBite.BuildingBlocks.Kafka;

public sealed class KafkaOptions
{
    public bool Enabled { get; set; } = true;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "quickbite";
    public string Producer { get; set; } = "quickbite";
    public short TopicReplicationFactor { get; set; } = 1;
    public int TopicPartitionCount { get; set; } = 1;
    public int TopicInitializationRetryCount { get; set; } = 5;
    public int TopicInitializationRetryDelaySeconds { get; set; } = 2;
    public KafkaTopics Topics { get; set; } = new();
    public KafkaConsumerOptions Consumer { get; set; } = new();
    public KafkaDeadLetterOptions DeadLetter { get; set; } = new();
}

public sealed class KafkaTopics
{
    public string OrderCreated { get; set; } = "quickbite.orders.order-created.v1";
    public string PaymentSucceeded { get; set; } = "quickbite.payments.payment-succeeded.v1";
    public string PaymentFailed { get; set; } = "quickbite.payments.payment-failed.v1";
    public string DeliveryAssigned { get; set; } = "quickbite.delivery.delivery-assigned.v1";
    public string DeliveryCompleted { get; set; } = "quickbite.delivery.delivery-completed.v1";

    public IReadOnlyCollection<string> All()
    {
        return
        [
            OrderCreated,
            PaymentSucceeded,
            PaymentFailed,
            DeliveryAssigned,
            DeliveryCompleted
        ];
    }
}

public sealed class KafkaConsumerOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBackoffMilliseconds { get; set; } = 500;
}

public sealed class KafkaDeadLetterOptions
{
    public bool Enabled { get; set; } = true;
    public string TopicSuffix { get; set; } = ".dlq";
}

public sealed class KafkaOptionsValidator : IValidateOptions<KafkaOptions>
{
    public ValidateOptionsResult Validate(string? name, KafkaOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            failures.Add("Kafka:BootstrapServers must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add("Kafka:ClientId must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Producer))
        {
            failures.Add("Kafka:Producer must be configured.");
        }

        if (options.TopicPartitionCount <= 0)
        {
            failures.Add("Kafka:TopicPartitionCount must be greater than zero.");
        }

        if (options.TopicReplicationFactor <= 0)
        {
            failures.Add("Kafka:TopicReplicationFactor must be greater than zero.");
        }

        if (options.TopicInitializationRetryCount < 0)
        {
            failures.Add("Kafka:TopicInitializationRetryCount cannot be negative.");
        }

        if (options.TopicInitializationRetryDelaySeconds < 0)
        {
            failures.Add("Kafka:TopicInitializationRetryDelaySeconds cannot be negative.");
        }

        if (options.Consumer.MaxRetryAttempts < 0)
        {
            failures.Add("Kafka:Consumer:MaxRetryAttempts cannot be negative.");
        }

        if (options.Consumer.RetryBackoffMilliseconds < 0)
        {
            failures.Add("Kafka:Consumer:RetryBackoffMilliseconds cannot be negative.");
        }

        if (options.DeadLetter.Enabled && string.IsNullOrWhiteSpace(options.DeadLetter.TopicSuffix))
        {
            failures.Add("Kafka:DeadLetter:TopicSuffix must be configured when dead-letter publishing is enabled.");
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

        if (missingTopics.Length > 0)
        {
            failures.Add($"Kafka topic names must be configured. Missing: {string.Join(", ", missingTopics)}.");
        }

        var duplicateTopics = topics.All()
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .GroupBy(topic => topic, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateTopics.Length > 0)
        {
            failures.Add($"Kafka topic names must be unique. Duplicates: {string.Join(", ", duplicateTopics)}.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

public interface IKafkaProducer
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent;

    Task PublishSerializedAsync(
        string topic,
        string messageKey,
        string envelopeJson,
        Guid eventId,
        string eventType,
        int eventVersion,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}

public sealed class NoOpKafkaProducer(ILogger<NoOpKafkaProducer> logger) : IKafkaProducer
{
    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        logger.LogInformation(
            "Kafka publishing is disabled. Skipping event {EventType} for topic {TopicName}.",
            message.EventType,
            topic);

        return Task.CompletedTask;
    }

    public Task PublishSerializedAsync(
        string topic,
        string messageKey,
        string envelopeJson,
        Guid eventId,
        string eventType,
        int eventVersion,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Kafka publishing is disabled. Skipping outbox event {EventType} for topic {TopicName}.",
            eventType,
            topic);

        return Task.CompletedTask;
    }
}

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly KafkaOptions _options;
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            LingerMs = 5
        }).Build();
    }

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        var envelope = CreateEnvelope(message, _options.Producer);
        var payload = JsonSerializer.Serialize(envelope, SerializerOptions);

        return _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = ResolveMessageKey(message, envelope.EventId),
            Value = payload,
            Headers =
            [
                new Header("event-id", envelope.EventId.ToString("N").ToUtf8Bytes()),
                new Header("event-type", envelope.EventType.ToUtf8Bytes()),
                new Header("event-version", envelope.EventVersion.ToString().ToUtf8Bytes()),
                new Header("correlation-id", envelope.CorrelationId.ToString("N").ToUtf8Bytes())
            ]
        }, cancellationToken);
    }

    public Task PublishSerializedAsync(
        string topic,
        string messageKey,
        string envelopeJson,
        Guid eventId,
        string eventType,
        int eventVersion,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        return _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = messageKey,
            Value = envelopeJson,
            Headers =
            [
                new Header("event-id", eventId.ToString("N").ToUtf8Bytes()),
                new Header("event-type", eventType.ToUtf8Bytes()),
                new Header("event-version", eventVersion.ToString().ToUtf8Bytes()),
                new Header("correlation-id", correlationId.ToString("N").ToUtf8Bytes())
            ]
        }, cancellationToken);
    }

    internal static EventEnvelope<T> CreateEnvelope<T>(T message, string producer)
        where T : IIntegrationEvent
    {
        return new EventEnvelope<T>(
            Guid.NewGuid(),
            message.EventType,
            message.EventVersion,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            null,
            producer,
            message);
    }

    internal static string ResolveMessageKey<T>(T message, Guid fallback)
    {
        var orderIdProperty = typeof(T).GetProperty("OrderId");
        var orderId = orderIdProperty?.GetValue(message);

        return orderId is Guid guid && guid != Guid.Empty
            ? guid.ToString("N")
            : fallback.ToString("N");
    }

    public void Dispose() => _producer.Dispose();
}

public abstract class KafkaConsumerBackgroundService<T> : BackgroundService where T : IIntegrationEvent
{
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
        if (!_options.Enabled)
        {
            _logger.LogInformation("Kafka consumer for topic {TopicName} is disabled in the current environment.", TopicName);
            return Task.CompletedTask;
        }

        return Task.Run(async () =>
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                ClientId = $"{_options.ClientId}-{GroupId}",
                GroupId = GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                ClientId = $"{_options.ClientId}-{GroupId}-dlq-producer",
                Acks = Acks.All,
                EnableIdempotence = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            using var deadLetterProducer = new ProducerBuilder<string, string>(producerConfig).Build();
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

                    var processed = await ProcessMessageAsync(result, deadLetterProducer, stoppingToken);
                    if (processed)
                    {
                        consumer.StoreOffset(result);
                        consumer.Commit(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException exception)
                {
                    _logger.LogError(exception, "Kafka consume error on topic {TopicName}.", TopicName);
                    await DelayAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Unexpected Kafka consumer failure on topic {TopicName}.", TopicName);
                    await DelayAsync(stoppingToken);
                }
            }
        }, stoppingToken);
    }

    private async Task<bool> ProcessMessageAsync(
        ConsumeResult<Ignore, string> result,
        IProducer<string, string> deadLetterProducer,
        CancellationToken cancellationToken)
    {
        EventEnvelope<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope<T>>(result.Message.Value, KafkaProducer.SerializerOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Kafka message on topic {TopicName} could not be deserialized.", TopicName);
            return await PublishDeadLetterAsync(result, deadLetterProducer, exception, cancellationToken);
        }

        if (envelope is null)
        {
            _logger.LogWarning("Kafka message on topic {TopicName} deserialized to null.", TopicName);
            return await PublishDeadLetterAsync(
                result,
                deadLetterProducer,
                new InvalidOperationException("Kafka message deserialized to null."),
                cancellationToken);
        }

        for (var attempt = 1; attempt <= _options.Consumer.MaxRetryAttempts + 1; attempt++)
        {
            try
            {
                await HandleAsync(envelope, cancellationToken);
                return true;
            }
            catch (Exception exception) when (attempt <= _options.Consumer.MaxRetryAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Kafka handler attempt {AttemptNumber} failed for event {EventType} from topic {TopicName}.",
                    attempt,
                    envelope.EventType,
                    TopicName);

                await DelayAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Kafka handler failed permanently for event {EventType} from topic {TopicName}.",
                    envelope.EventType,
                    TopicName);

                return await PublishDeadLetterAsync(result, deadLetterProducer, exception, cancellationToken);
            }
        }

        return false;
    }

    private async Task<bool> PublishDeadLetterAsync(
        ConsumeResult<Ignore, string> result,
        IProducer<string, string> deadLetterProducer,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!_options.DeadLetter.Enabled)
        {
            return false;
        }

        var deadLetterTopic = $"{TopicName}{_options.DeadLetter.TopicSuffix}";
        var deadLetter = new DeadLetterMessage(
            TopicName,
            result.Partition.Value,
            result.Offset.Value,
            GroupId,
            exception.GetType().Name,
            exception.Message,
            DateTimeOffset.UtcNow,
            result.Message.Value);

        await deadLetterProducer.ProduceAsync(deadLetterTopic, new Message<string, string>
        {
            Key = $"{TopicName}-{result.Partition.Value}-{result.Offset.Value}",
            Value = JsonSerializer.Serialize(deadLetter, KafkaProducer.SerializerOptions)
        }, cancellationToken);

        _logger.LogWarning(
            "Kafka message from topic {TopicName} offset {Offset} was moved to dead-letter topic {DeadLetterTopic}.",
            TopicName,
            result.Offset.Value,
            deadLetterTopic);

        return true;
    }

    private Task DelayAsync(CancellationToken cancellationToken)
    {
        return _options.Consumer.RetryBackoffMilliseconds == 0
            ? Task.CompletedTask
            : Task.Delay(TimeSpan.FromMilliseconds(_options.Consumer.RetryBackoffMilliseconds), cancellationToken);
    }
}

public sealed record DeadLetterMessage(
    string SourceTopic,
    int SourcePartition,
    long SourceOffset,
    string ConsumerGroup,
    string ErrorType,
    string ErrorMessage,
    DateTimeOffset FailedAtUtc,
    string RawMessage);

public abstract class OutboxPublisherBackgroundService<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(10);

    protected OutboxPublisherBackgroundService(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected virtual int BatchSize => 25;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox publisher failed while processing a batch.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();
        var now = DateTimeOffset.UtcNow;

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(x => x.PublishedAtUtc == null && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await producer.PublishSerializedAsync(
                    message.TopicName,
                    message.MessageKey,
                    message.EnvelopeJson,
                    message.EventId,
                    message.EventType,
                    message.EventVersion,
                    message.CorrelationId,
                    cancellationToken);

                message.MarkPublished(DateTimeOffset.UtcNow);
            }
            catch (Exception exception)
            {
                message.MarkFailed(exception.Message, DateTimeOffset.UtcNow.Add(_retryDelay));
                _logger.LogWarning(
                    exception,
                    "Outbox event {EventId} failed to publish to topic {TopicName}.",
                    message.EventId,
                    message.TopicName);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var kafkaOptions = options.Value;
        if (!kafkaOptions.Enabled)
        {
            return;
        }

        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = kafkaOptions.BootstrapServers,
            ClientId = $"{kafkaOptions.ClientId}-topic-initializer"
        }).Build();

        var topicNames = kafkaOptions.Topics.All()
            .Concat(kafkaOptions.DeadLetter.Enabled
                ? kafkaOptions.Topics.All().Select(topic => $"{topic}{kafkaOptions.DeadLetter.TopicSuffix}")
                : [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var specifications = topicNames.Select(topic => new TopicSpecification
        {
            Name = topic,
            NumPartitions = kafkaOptions.TopicPartitionCount,
            ReplicationFactor = kafkaOptions.TopicReplicationFactor
        });

        for (var attempt = 1; attempt <= kafkaOptions.TopicInitializationRetryCount + 1; attempt++)
        {
            try
            {
                await adminClient.CreateTopicsAsync(specifications, new CreateTopicsOptions
                {
                    RequestTimeout = TimeSpan.FromSeconds(15)
                });
                return;
            }
            catch (CreateTopicsException exception) when (AllTopicsAlreadyExist(exception))
            {
                logger.LogInformation("Kafka topics already exist.");
                return;
            }
            catch (Exception exception) when (attempt <= kafkaOptions.TopicInitializationRetryCount && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "Kafka topic initialization attempt {AttemptNumber} failed. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    kafkaOptions.TopicInitializationRetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(kafkaOptions.TopicInitializationRetryDelaySeconds), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool AllTopicsAlreadyExist(CreateTopicsException exception)
    {
        return exception.Results.All(result =>
            result.Error.Code is ErrorCode.TopicAlreadyExists or ErrorCode.NoError);
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
        services.AddSingleton<IKafkaProducer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return options.Enabled
                ? new KafkaProducer(serviceProvider.GetRequiredService<IOptions<KafkaOptions>>())
                : new NoOpKafkaProducer(serviceProvider.GetRequiredService<ILogger<NoOpKafkaProducer>>());
        });
        services.AddHostedService<KafkaTopicInitializer>();
        return services;
    }
}

internal static class KafkaStringExtensions
{
    public static byte[] ToUtf8Bytes(this string value) => System.Text.Encoding.UTF8.GetBytes(value);
}

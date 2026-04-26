using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace QuickBite.BuildingBlocks.Common;

public static class QuickBiteTelemetry
{
    public const string ActivitySourceName = "QuickBite";
    public const string MeterName = "QuickBite";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> KafkaMessagesPublished = Meter.CreateCounter<long>(
        "quickbite.kafka.messages.published",
        description: "Number of Kafka messages successfully published.");

    public static readonly Counter<long> KafkaPublishFailures = Meter.CreateCounter<long>(
        "quickbite.kafka.publish.failures",
        description: "Number of Kafka publish failures.");

    public static readonly Counter<long> KafkaMessagesConsumed = Meter.CreateCounter<long>(
        "quickbite.kafka.messages.consumed",
        description: "Number of Kafka messages successfully handled by consumers.");

    public static readonly Counter<long> KafkaHandlerRetries = Meter.CreateCounter<long>(
        "quickbite.kafka.handler.retries",
        description: "Number of Kafka handler retry attempts.");

    public static readonly Counter<long> KafkaDeadLetters = Meter.CreateCounter<long>(
        "quickbite.kafka.deadletters",
        description: "Number of Kafka messages sent to dead-letter topics.");

    public static readonly Histogram<long> KafkaConsumerLag = Meter.CreateHistogram<long>(
        "quickbite.kafka.consumer.lag",
        unit: "messages",
        description: "Estimated Kafka consumer lag observed while consuming messages.");

    public static readonly Counter<long> OutboxMessagesPublished = Meter.CreateCounter<long>(
        "quickbite.outbox.messages.published",
        description: "Number of outbox messages successfully published.");

    public static readonly Counter<long> OutboxPublishFailures = Meter.CreateCounter<long>(
        "quickbite.outbox.publish.failures",
        description: "Number of outbox message publish failures.");

    public static KeyValuePair<string, object?>[] Tags(params string[] keyValues)
    {
        if (keyValues.Length % 2 != 0)
        {
            throw new ArgumentException("Tags must be supplied as key/value pairs.", nameof(keyValues));
        }

        var tags = new KeyValuePair<string, object?>[keyValues.Length / 2];
        for (var index = 0; index < keyValues.Length; index += 2)
        {
            tags[index / 2] = new KeyValuePair<string, object?>(keyValues[index], keyValues[index + 1]);
        }

        return tags;
    }
}

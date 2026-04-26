using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuickBite.BuildingBlocks.Contracts;

namespace QuickBite.BuildingBlocks.Common;

public sealed class OutboxMessage : Entity
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string TopicName { get; private set; } = string.Empty;
    public string MessageKey { get; private set; } = string.Empty;
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public int EventVersion { get; private set; }
    public Guid CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }
    public string Producer { get; private set; } = string.Empty;
    public string EnvelopeJson { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public int PublishAttempts { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create<T>(
        string topicName,
        T integrationEvent,
        string producer,
        Guid? correlationId = null,
        Guid? causationId = null)
        where T : IIntegrationEvent
    {
        var envelope = new EventEnvelope<T>(
            Guid.NewGuid(),
            integrationEvent.EventType,
            integrationEvent.EventVersion,
            DateTimeOffset.UtcNow,
            correlationId ?? Guid.NewGuid(),
            causationId,
            producer,
            integrationEvent);

        return Create(topicName, ResolveMessageKey(integrationEvent, envelope.EventId), envelope);
    }

    public static OutboxMessage Create<T>(string topicName, string messageKey, EventEnvelope<T> envelope)
        where T : IIntegrationEvent
    {
        return new OutboxMessage
        {
            TopicName = topicName,
            MessageKey = messageKey,
            EventId = envelope.EventId,
            EventType = envelope.EventType,
            EventVersion = envelope.EventVersion,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.CausationId,
            Producer = envelope.Producer,
            EnvelopeJson = JsonSerializer.Serialize(envelope, SerializerOptions),
            OccurredAtUtc = envelope.OccurredAtUtc
        };
    }

    public void MarkPublished(DateTimeOffset publishedAtUtc)
    {
        PublishedAtUtc = publishedAtUtc;
        LastError = null;
        NextAttemptAtUtc = null;
        Touch();
    }

    public void MarkFailed(string error, DateTimeOffset retryAfterUtc)
    {
        PublishAttempts++;
        LastError = error.Length > 1_000 ? error[..1_000] : error;
        NextAttemptAtUtc = retryAfterUtc;
        Touch();
    }

    private static string ResolveMessageKey<T>(T integrationEvent, Guid fallback)
    {
        var orderIdProperty = typeof(T).GetProperty("OrderId");
        var orderId = orderIdProperty?.GetValue(integrationEvent);

        return orderId is Guid guid && guid != Guid.Empty
            ? guid.ToString("N")
            : fallback.ToString("N");
    }
}

public sealed class InboxMessage : Entity
{
    public Guid EventId { get; private set; }
    public string Consumer { get; private set; } = string.Empty;
    public string TopicName { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    private InboxMessage()
    {
    }

    public InboxMessage(Guid eventId, string consumer, string topicName, string eventType)
    {
        EventId = eventId;
        Consumer = consumer;
        TopicName = topicName;
        EventType = eventType;
        ReceivedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        Touch();
    }
}

public static class ReliabilityModelBuilderExtensions
{
    public static void ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => new { x.PublishedAtUtc, x.NextAttemptAtUtc, x.CreatedAtUtc });
            entity.Property(x => x.TopicName).HasMaxLength(250);
            entity.Property(x => x.MessageKey).HasMaxLength(100);
            entity.Property(x => x.EventType).HasMaxLength(100);
            entity.Property(x => x.Producer).HasMaxLength(150);
            entity.Property(x => x.EnvelopeJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.LastError).HasMaxLength(1_000);
        });
    }

    public static void ConfigureInbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EventId, x.Consumer }).IsUnique();
            entity.Property(x => x.Consumer).HasMaxLength(150);
            entity.Property(x => x.TopicName).HasMaxLength(250);
            entity.Property(x => x.EventType).HasMaxLength(100);
        });
    }
}

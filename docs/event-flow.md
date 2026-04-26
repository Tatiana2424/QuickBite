# Event Flow

## Order lifecycle

1. A client creates an order through the Orders API.
2. The Orders service stores the order and an `order.created` outbox record in the same database transaction.
3. The Orders outbox publisher publishes `order.created` to the versioned order-created topic.
4. The Payments service consumes `order.created`, records the event in its inbox, stores the payment result, and writes `payment.succeeded` or `payment.failed` to its outbox in the same transaction.
5. The Payments outbox publisher emits the payment result event.
6. The Orders service consumes payment result events and updates order status to `Confirmed` or `Failed`.
7. The Delivery service consumes `payment.succeeded`, records the event in its inbox, creates a delivery, assigns a courier, and writes `delivery.assigned` to its outbox.
8. The Delivery outbox publisher emits `delivery.assigned`.

## Topics used in the starter

- `quickbite.orders.order-created.v1`
- `quickbite.payments.payment-succeeded.v1`
- `quickbite.payments.payment-failed.v1`
- `quickbite.delivery.delivery-assigned.v1`
- `quickbite.delivery.delivery-completed.v1`

Every topic also has a dead-letter companion using the `.dlq` suffix, for example `quickbite.orders.order-created.v1.dlq`.

## Envelope contract

All messages are wrapped in an `EventEnvelope<T>` with:

- `eventId`: unique event identifier.
- `eventType`: stable semantic event name such as `order.created`.
- `eventVersion`: event schema version, currently `1`.
- `occurredAtUtc`: UTC creation timestamp.
- `correlationId`: request/workflow correlation identifier.
- `causationId`: reserved for linking follow-up events to their source event.
- `producer`: service that emitted the event.
- `payload`: strongly typed integration event payload.

Kafka messages use the order id as the message key when the event contains `OrderId`. This keeps order-lifecycle events partitioned consistently as the platform grows.

## Reliability baseline

The current implementation includes:

- idempotent Kafka producers with `acks=all`;
- transactional outbox tables for Orders, Payments, and Delivery;
- inbox/processed-message tracking for payment and delivery consumers;
- idempotency-key support for externally retriable order creation;
- status-history tables for order, payment, and delivery state transitions;
- startup topic initialization with bounded retry for local/container development;
- manual consumer offset commits after successful handling;
- bounded retry attempts with configurable backoff;
- dead-letter publishing for deserialization and permanent handler failures.

## Recovery model

- If a service saves business data but crashes before publishing Kafka, the unpublished row remains in `OutboxMessages` and is retried by the service outbox publisher.
- If Kafka delivers a duplicate event, the consumer checks `InboxMessages` by `(EventId, Consumer)` and skips already processed messages.
- If a handler fails after retries, the Kafka consumer moves the raw message to the matching `.dlq` topic for inspection and replay planning.
- Order, payment, and delivery status history tables provide an audit trail for reconciliation jobs and manual support workflows.

## Scope of the current version

- The saga is choreographed through events rather than orchestrated by a process manager.
- Outbox publishing uses polling and bounded retry state; advanced leasing/partitioned dispatch can be added later if multiple replicas publish the same service outbox.
- Reconciliation is documented through status history and durable message state, but a dedicated scheduled reconciliation job is deferred.
- Schema registry and contract compatibility checks are deferred until message contracts become more complex.

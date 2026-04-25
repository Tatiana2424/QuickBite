# Event Flow

## Order lifecycle

1. A client creates an order through the Orders API.
2. The Orders service stores the order and publishes `order.created` to the versioned order-created topic.
3. The Payments service consumes `order.created` and decides whether the simulated payment succeeds.
4. A successful payment emits `payment.succeeded`; a failure emits `payment.failed`.
5. The Delivery service consumes `payment.succeeded`, creates a delivery, assigns a courier, and emits `delivery.assigned`.

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
- startup topic initialization with bounded retry for local/container development;
- manual consumer offset commits after successful handling;
- bounded retry attempts with configurable backoff;
- dead-letter publishing for deserialization and permanent handler failures.

## Scope of the first version

The current implementation intentionally does not include the full production reliability stack yet:

- Orders still publishes directly after persistence instead of using a transactional outbox.
- Consumers do not yet persist inbox/deduplication state.
- There is no saga/process-manager orchestration yet.
- Schema registry and contract compatibility checks are deferred until message contracts become more complex.

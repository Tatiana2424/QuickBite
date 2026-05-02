# Kafka Architecture

Kafka is used for asynchronous workflows where services should not call each other synchronously or share databases. In the current QuickBite lifecycle, Orders publishes facts about order creation, Payments reacts to those facts, and Delivery reacts to successful payment events.

## Why Kafka is needed

- Order processing crosses service boundaries and should tolerate temporary downstream outages.
- Payments and Delivery should be independently deployable and retry their own work.
- The event stream becomes an integration boundary for future features such as notifications, analytics, and courier assignment.

Kafka is not currently used for read-only Catalog or Identity flows.

## Topic design

Topics are named by platform, producing bounded context, event name, and schema version:

- `quickbite.orders.order-created.v1`
- `quickbite.payments.payment-succeeded.v1`
- `quickbite.payments.payment-failed.v1`
- `quickbite.delivery.delivery-assigned.v1`
- `quickbite.delivery.delivery-completed.v1`

Dead-letter topics append `.dlq` to the source topic name.

## Producer responsibilities

- Wrap every integration event in `EventEnvelope<T>`.
- Include event id, event type, event version, producer name, correlation id, optional causation id, timestamp, and payload.
- Use the order id as the message key when available.
- Use idempotent producer settings with `acks=all`.

## Consumer responsibilities

- Disable auto-commit and commit offsets only after successful handling or dead-letter publishing.
- Retry transient handler failures with a bounded retry count and backoff.
- Publish malformed or permanently failed messages to the matching dead-letter topic.
- Keep consumer group ids service-specific so each owning service receives the events it needs.

## Local setup

Docker-backed modes run Kafka and Kafka UI through Compose. Host-mode Windows startup keeps Kafka disabled by default so the full site remains runnable without Docker. To exercise Kafka end-to-end, use the full-stack Compose flow:

`docker compose -f docker-compose.yml -f docker-compose.fullstack.yml up --build`

Kafka UI is available at `http://localhost:8085`.

APIs initialize topics on startup when Kafka is enabled and use bounded retry so container startup ordering is less fragile while Kafka is becoming reachable.

## Known next reliability work

- Add an outbox pattern to Orders and Payments so database commits and event publishing are coordinated.
- Add inbox/deduplication tables for idempotent consumers.
- Add contract compatibility tests before changing event payloads.
- Tune observability thresholds for consumer lag, DLQ counts, and retry exhaustion after real traffic exists.

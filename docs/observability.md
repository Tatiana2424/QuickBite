# Observability And Operability

QuickBite uses a practical local observability stack that can later be replaced or extended with a managed provider.

## Runtime Signals

Every backend service and the gateway expose:

- `/health`: aggregate health with JSON details.
- `/health/live`: liveness only, intended for process-level checks.
- `/health/ready`: readiness checks, including database connectivity for services with a database.
- `/metrics`: Prometheus scrape endpoint.

Structured logs include:

- `Application`
- `CorrelationId`
- `TraceId`
- HTTP method, path, status code, and elapsed time
- exception details when failures occur

## OpenTelemetry

The shared observability building block configures ASP.NET Core tracing and metrics, HTTP client tracing and metrics, runtime metrics, QuickBite custom metrics, Prometheus scraping, and optional OTLP export through `OTEL_EXPORTER_OTLP_ENDPOINT`.

Kafka publishing and consuming use the shared `QuickBite` activity source. Message spans include Kafka topic, consumer group, partition, offset, event type, event id, and correlation id where available.

## Prometheus And Grafana

Local Docker Compose includes:

- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3001`

Default Grafana credentials for local development are `admin` / `quickbite`.

The starter dashboard is provisioned from `infra/observability/grafana/provisioning/dashboards/quickbite-overview.json`.

## Important Metrics

- Request rate: `http_server_request_duration_seconds_count`
- Request latency: `http_server_request_duration_seconds_bucket`
- Kafka published messages: `quickbite_kafka_messages_published_total`
- Kafka publish failures: `quickbite_kafka_publish_failures_total`
- Kafka consumed messages: `quickbite_kafka_messages_consumed_total`
- Kafka handler retries: `quickbite_kafka_handler_retries_total`
- Kafka dead letters: `quickbite_kafka_deadletters_total`
- Kafka consumer lag: `quickbite_kafka_consumer_lag_bucket`
- Outbox published messages: `quickbite_outbox_messages_published_total`
- Outbox publish failures: `quickbite_outbox_publish_failures_total`

## Alert Starting Points

The local alert file defines starter rules for scrape target down, HTTP 5xx error ratio above 5%, HTTP p95 latency above 1 second, Kafka dead-letter messages, elevated Kafka handler retries, high observed Kafka consumer lag, and outbox publish failures.

These are local defaults. Thresholds should be tuned after the application has real traffic and product expectations.

## Runbook Starters

### Service Down

1. Check Grafana target health and Prometheus `up` for the service.
2. Open the service logs and filter by `Application` and `CorrelationId`.
3. Check `/health/ready` for database connectivity failures.
4. Restart only the failing service if dependency checks are healthy.

### High HTTP Error Rate

1. Use the dashboard to identify the service with the highest 5xx ratio.
2. Search logs by `Application` and `TraceId` for exceptions.
3. Check recent deploy or migration changes.
4. Confirm downstream dependencies with `/health/ready`.

### Kafka Retries Or DLQ

1. Open Kafka UI at `http://localhost:8085`.
2. Inspect the source topic and matching `.dlq` topic.
3. Use the event id and correlation id from logs to trace the workflow.
4. Check consumer service logs for retry errors.
5. Fix the handler/data problem before replaying messages.

### Outbox Publish Failures

1. Check Kafka availability and topic existence.
2. Inspect service logs for outbox publisher warnings.
3. Query the service database `OutboxMessages` table for rows with `LastError`.
4. After Kafka recovers, verify `PublishedAtUtc` is set by the outbox publisher.

### Database Readiness Failure

1. Open `/health/ready` for the failing service.
2. Verify SQL Server container health and connection string configuration.
3. Check whether migrations are blocked or still running.
4. Confirm the service-specific database exists.

## Ownership

For the pet-project phase, dashboard and alert ownership belongs to the repository owner. As the project grows, ownership should move to service owners for Identity, Catalog, Orders, Payments, and Delivery signals.

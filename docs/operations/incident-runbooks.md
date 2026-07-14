# Incident Runbooks

Use these runbooks with the observability signals in `docs/observability.md`.

## Severity

| Severity | Definition | Response target |
| --- | --- | --- |
| SEV1 | Public ordering, login, or payment workflow unavailable | Acknowledge in 15 minutes |
| SEV2 | Degraded critical workflow, elevated errors, or data recovery risk | Acknowledge in 30 minutes |
| SEV3 | Non-critical service degradation or operational alert without customer impact | Acknowledge next business day |

## Incident Roles

- Incident commander: owns timeline, decisions, and communication.
- Technical lead: investigates and executes mitigation.
- Communications lead: posts stakeholder updates.
- Scribe: records timestamps, commands, dashboards, and decisions.

For a small team, one person may hold multiple roles, but the incident commander should be explicit.

## Common Response Steps

1. Confirm the alert with dashboard data and `/health/ready`.
2. Assign severity and incident commander.
3. Check the latest deployment, migration, configuration, or secret change.
4. Mitigate first: rollback, scale, restart, disable traffic, or fail over.
5. Preserve evidence: logs, traces, metrics, event ids, offsets, and release SHA.
6. Communicate status until the incident is resolved.
7. Write a post-incident review for SEV1 and SEV2 incidents.

## Gateway High 5xx Or Latency

Signals:

- Gateway 5xx ratio above launch SLO.
- Gateway p95 latency above target.
- Ingress or gateway readiness failures.

Actions:

1. Check ingress controller health and TLS certificate status.
2. Check gateway logs by `TraceId` and `CorrelationId`.
3. Verify downstream service `/health/ready` endpoints.
4. Compare current image tag to last known-good tag.
5. Roll back gateway first if only routing/security configuration changed.
6. Scale gateway replicas if CPU or memory is saturated and downstreams are healthy.

## Database Readiness Failure

Signals:

- Service `/health/ready` unhealthy.
- SQL connection timeout or login failures.
- Elevated request latency and connection pool saturation.

Actions:

1. Confirm whether the provider database is reachable from the cluster.
2. Verify the service secret contains the expected connection string.
3. Check recent migration bundle execution.
4. If migration failed, stop the affected deployment and restore from backup if required.
5. If provider is degraded, fail over according to the provider runbook.
6. After recovery, run read/write smoke tests for the owning service.

## Kafka Consumer Lag Or DLQ Growth

Signals:

- `quickbite_kafka_consumer_lag_bucket` elevated.
- `quickbite_kafka_deadletters_total` increasing.
- Handler retry alerts.

Actions:

1. Identify topic, partition, consumer group, and first failing offset.
2. Inspect service logs for handler exceptions and event ids.
3. Scale consumer service only if failures are capacity-related.
4. If failures are data or code related, pause rollout or scale the consumer to zero.
5. Patch the handler or data issue.
6. Reprocess using the replay checklist in `docs/operations/backup-disaster-recovery.md`.

## Outbox Backlog

Signals:

- `quickbite_outbox_publish_failures_total` increasing.
- Unpublished `OutboxMessages` rows older than the retry window.

Actions:

1. Check Kafka availability and topic existence.
2. Check producer configuration and secret/config changes.
3. Query the owning service outbox for `LastError`.
4. Restore Kafka connectivity.
5. Confirm `PublishedAtUtc` is populated as the backlog drains.

## Security Incident

Signals:

- Exposed token, leaked secret, suspicious GitHub activity, or critical dependency advisory.

Actions:

1. Revoke exposed credentials immediately.
2. Rotate affected secrets in the secret store and Kubernetes secret.
3. Redeploy affected workloads so rotated values are loaded.
4. Review GitHub audit log, deployment history, and access tokens.
5. Patch dependencies or roll back affected images.
6. Record accepted risk only when immediate remediation is not possible.

## Post-Incident Review

Capture:

- Customer impact and timeline.
- Detection source.
- Root cause and contributing factors.
- What went well and what was confusing.
- Follow-up actions with owners and due dates.
- Whether SLO error budget was consumed.

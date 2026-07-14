# Backup And Disaster Recovery

QuickBite uses one database per service. Each database must be backed up, restored, and validated independently so a service can be recovered without crossing ownership boundaries.

## Databases

| Service | Database | Backup priority |
| --- | --- | --- |
| Identity | `QuickBiteIdentityDb` | P0 |
| Catalog | `QuickBiteCatalogDb` | P1 |
| Orders | `QuickBiteOrdersDb` | P0 |
| Payments | `QuickBitePaymentsDb` | P0 |
| Delivery | `QuickBiteDeliveryDb` | P1 |

## Backup Policy

Production target:

- Full backup daily.
- Differential backup every 6 hours when provider support exists.
- Transaction log backup every 15 minutes for P0 databases.
- Backup encryption enabled at rest.
- Cross-zone or cross-region backup copy enabled when supported by the hosting provider.
- Retention: 35 days online, 12 monthly recovery points for compliance/audit needs.

Staging target:

- Full backup daily.
- Retention: 7 days.
- Restore rehearsals may use masked production snapshots only if privacy requirements allow it.

## Restore Rehearsal

Run at least monthly and before launch:

1. Select the latest backup for one service database.
2. Restore into an isolated staging or recovery namespace.
3. Apply pending migration bundles if the backup predates the current release.
4. Start only the owning service and its required dependencies.
5. Open `/health/ready` for the owning service.
6. Execute a read smoke test for restored data.
7. For Orders, Payments, and Delivery, verify outbox and inbox tables are present and can be queried.
8. Record restore start time, restore finish time, validation result, backup timestamp, and operator.

Repeat for all five service databases before go-live approval.

## Recovery Objectives

Initial assumptions until the hosting provider and business requirements are finalized:

| Scenario | RTO | RPO |
| --- | --- | --- |
| Single API pod failure | 5 minutes | 0 minutes |
| Single service database restore | 60 minutes | 15 minutes for P0, 24 hours for P1 |
| Kafka broker/topic recovery | 60 minutes | Event retention window |
| Region-level platform failure | 4 hours | 60 minutes |
| Full environment rebuild | 8 hours | Latest verified backup plus retained Kafka events |

## Disaster Recovery Flow

1. Declare severity and assign incident commander.
2. Freeze deployments unless a rollback is the mitigation.
3. Identify the failing tier: ingress, gateway, service pod, database, Kafka, or external provider.
4. Restore service databases in dependency order when data recovery is required:
   1. Identity
   2. Catalog
   3. Orders
   4. Payments
   5. Delivery
5. Apply migration bundles matching the target application image set.
6. Deploy the last known-good immutable image tags.
7. Validate `/health/ready`, gateway routes, and critical workflows.
8. Reconcile Kafka/outbox state before reopening traffic.
9. Record incident timeline, data-loss window, and follow-up actions.

## Kafka Retention And Recovery

Recommended production topic defaults:

- Workflow topics: 7 days retention.
- Dead-letter topics: 14 days retention.
- Replication factor: 3.
- Minimum in-sync replicas: 2.
- Producers use idempotent publishing with `acks=all`.

Recovery guidance:

- If a consumer is down, keep producers running if the topic retention window is sufficient and lag is monitored.
- If a handler bug creates DLQ messages, stop the affected consumer deployment or scale it to zero before replay.
- Fix the data or code issue first, then replay only the affected event range.
- Preserve original event ids and correlation ids during replay so inbox deduplication can protect already-processed messages.
- Do not truncate production topics without an approved data-recovery plan.

## Replay Checklist

1. Identify source topic, DLQ topic, partitions, offsets, event ids, and correlation ids.
2. Confirm the handler defect or bad data has been fixed.
3. Confirm replay will not violate idempotency expectations.
4. Back up affected service databases before replaying a large event set.
5. Replay into the original topic or a controlled replay topic, depending on tooling.
6. Watch consumer lag, handler retries, DLQ writes, and business state.
7. Document replay offsets, operator, start/end time, and validation result.

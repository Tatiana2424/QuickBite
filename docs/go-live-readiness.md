# Go-Live Readiness

QuickBite is production-ready only when the deployment package is paired with operational controls, tested recovery procedures, capacity evidence, security review, and clear support ownership. This document is the launch gate for staging and production.

## Readiness Gates

Before production launch:

- Staging runs the same Helm chart, image tags, secret model, ingress routing, and managed dependencies planned for production.
- Migration bundles have been applied and verified in staging using production-like data volume.
- Database backup and restore rehearsals have completed for every service database.
- Kafka retention, dead-letter inspection, and replay procedures have been rehearsed with test events.
- Critical workflows have passed load and soak tests with agreed latency and error-rate targets.
- Security and dependency scans have no unresolved critical or high findings without an accepted risk.
- On-call ownership, escalation paths, and incident runbooks are documented.
- Launch and rollback checklists are completed and approved.

## Service Criticality

| Component | Production role | Recovery priority |
| --- | --- | --- |
| Gateway | Public API entry point and rate-limited routing layer | P0 |
| Identity | Registration, login, token refresh, and role ownership | P0 |
| Orders | Order creation and lifecycle state | P0 |
| Payments | Payment state and payment-result event production | P0 |
| Delivery | Delivery assignment and delivery status | P1 |
| Catalog | Restaurant and menu read path | P1 |
| Web | Customer-facing static frontend | P1 |
| Kafka | Event delivery for order, payment, and delivery workflows | P0 |
| SQL Server | Source of truth for each service database | P0 |

## SLI And SLO Targets

These launch targets are placeholders until product traffic expectations are defined. Revisit them after the first stable staging soak and again after the first production traffic week.

| Area | SLI | Initial SLO |
| --- | --- | --- |
| Gateway availability | Percentage of non-5xx gateway responses | 99.5% monthly |
| API availability | Percentage of non-5xx service responses | 99.5% monthly for P0 APIs |
| Gateway latency | p95 HTTP request duration | <= 750 ms for read paths, <= 1500 ms for command paths |
| Order creation | Successful `POST /orders/api/orders` responses | 99% over 30 minutes |
| Async workflow | Order-created to payment-result processing time | p95 <= 60 seconds |
| Kafka health | Consumer lag on active workflow topics | p95 lag below 1,000 messages for 15 minutes |
| Database recovery | Restore validation for each service database | Last successful restore rehearsal within 30 days |
| Security hygiene | Critical/high dependency findings | 0 unresolved without risk acceptance |

## Capacity And Soak Testing

Run capacity validation from a production-like staging environment after migrations and secrets are in place.

### Critical Workflows

- Anonymous catalog browse through the web app and gateway.
- User registration, login, token refresh, and logout.
- Order creation with multiple line items.
- Order status read after payment success.
- Payment failure path and order compensation.
- Delivery assignment after payment success.

### Load Test Profile

Start with conservative assumptions until expected traffic is known:

- Warm-up: 5 minutes at 10 virtual users.
- Baseline: 30 minutes at 50 virtual users.
- Stress: 15 minutes ramp from 50 to 200 virtual users.
- Spike: 5 minutes at 300 virtual users.
- Soak: 4 hours at the highest baseline rate that keeps SLOs green.

Capture:

- Request rate, p50/p95/p99 latency, and 4xx/5xx rates by route.
- CPU, memory, restarts, and HPA scaling events by workload.
- SQL query latency, connection saturation, deadlocks, and backup impact.
- Kafka producer errors, consumer lag, handler retries, and DLQ writes.
- Outbox backlog and publish failure counts.

Exit criteria:

- No P0 API has sustained 5xx rate above 1%.
- Gateway p95 stays within the launch SLO for the baseline and soak phases.
- No pod enters a crash loop or restarts repeatedly.
- Kafka lag drains after stress/spike phases without manual intervention.
- Database connections remain below provider limits with headroom for failover.

## Disaster Recovery Targets

Initial recovery targets are documented in `docs/operations/backup-disaster-recovery.md`. For launch approval, every P0 path must have an agreed recovery time objective (RTO), recovery point objective (RPO), backup owner, and restore rehearsal record.

## Security Review

Complete before launch:

- Review `docs/security-baseline.md` and confirm no development signing keys or demo secrets are enabled in staging or production.
- Run the release workflow dependency scan and image scan.
- Run `dotnet list QuickBite.sln package --vulnerable --include-transitive`.
- Run `npm audit --audit-level=moderate` in `frontend/quickbite-web`.
- Confirm GitHub environment protection exists for production promotion.
- Confirm secret rotation owners and emergency revocation steps are known.
- Confirm CORS allowed origins, gateway rate limits, and TLS issuer settings match the target environment.
- Confirm container images use immutable `git-<commit-sha>` tags.

## Support Ownership

| Area | Primary owner | Escalation |
| --- | --- | --- |
| Application services | Repository owner until service teams exist | Platform/operator |
| SQL databases and backups | Platform/operator | Repository owner |
| Kafka topics and replay | Platform/operator | Repository owner |
| CI/CD and releases | Repository owner | Platform/operator |
| Security findings | Repository owner | Platform/operator |
| Customer-impacting incidents | On-call responder | Repository owner |

Every production deployment must name the on-call responder, release owner, and rollback approver in the release notes.

## Approval Record

For each launch candidate, record:

- Release SHA and image tags.
- Migration bundle artifact names and execution timestamps.
- Backup restore rehearsal timestamps for all service databases.
- Load/soak test report location.
- Security review result and accepted risks.
- Launch checklist approver.
- Rollback checklist approver.

Keep the approval record with the release notes or GitHub deployment summary.

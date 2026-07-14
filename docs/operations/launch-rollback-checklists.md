# Launch And Rollback Checklists

Use these checklists for every production launch candidate. Keep the completed copy with release notes or GitHub deployment records.

## Launch Checklist

### Release Package

- [ ] Release SHA is merged to `main`.
- [ ] Images exist in GHCR with immutable `git-<commit-sha>` tags.
- [ ] Migration bundles are attached to the release workflow artifacts.
- [ ] Helm chart renders with production values.
- [ ] Production namespace and runtime secret exist.

### Data And Messaging

- [ ] Latest backup exists for each service database.
- [ ] Restore rehearsal completed for Identity.
- [ ] Restore rehearsal completed for Catalog.
- [ ] Restore rehearsal completed for Orders.
- [ ] Restore rehearsal completed for Payments.
- [ ] Restore rehearsal completed for Delivery.
- [ ] Kafka topic retention and replication settings are confirmed.
- [ ] DLQ inspection and replay procedure has been rehearsed.

### Validation

- [ ] Backend tests passed.
- [ ] Frontend tests passed.
- [ ] Playwright smoke tests passed.
- [ ] Docker Compose config validation passed.
- [ ] Helm lint/template validation passed.
- [ ] Dependency and image scans completed.
- [ ] Load test report reviewed.
- [ ] Soak test report reviewed.
- [ ] No unresolved critical/high security findings remain without accepted risk.

### Production Readiness

- [ ] DNS points to the production ingress controller.
- [ ] TLS certificate is issued and valid.
- [ ] Gateway CORS allowed origins match production.
- [ ] Production environment protection and reviewer rules are configured.
- [ ] On-call responder is named.
- [ ] Incident commander backup is named.
- [ ] Stakeholder communication channel is ready.
- [ ] Rollback approver is named.

### Launch Execution

- [ ] Freeze unrelated deploys.
- [ ] Apply service database migrations in documented order.
- [ ] Deploy Helm release with immutable image tag.
- [ ] Verify pods are ready and HPAs are healthy.
- [ ] Verify gateway `/health/ready`.
- [ ] Verify web homepage.
- [ ] Verify login and token refresh.
- [ ] Verify catalog read.
- [ ] Verify order creation.
- [ ] Verify payment-result processing.
- [ ] Verify delivery assignment.
- [ ] Watch dashboards for at least 60 minutes after opening traffic.

## Rollback Checklist

- [ ] Declare rollback decision owner and timestamp.
- [ ] Identify last known-good image tag set.
- [ ] Confirm whether migrations are backward-compatible.
- [ ] If migrations are not backward-compatible, execute the compensating migration or restore plan.
- [ ] Pause non-essential deploys and traffic changes.
- [ ] Roll Helm release back to the last known-good values and image tags.
- [ ] Verify pod readiness.
- [ ] Verify gateway `/health/ready`.
- [ ] Verify login, catalog read, order creation, payment result, and delivery assignment.
- [ ] Check Kafka consumer lag and DLQ growth.
- [ ] Check outbox backlog.
- [ ] Communicate rollback completion.
- [ ] Open follow-up incident review for SEV1/SEV2 rollback.

## Approval

| Role | Name | Date | Approval |
| --- | --- | --- | --- |
| Release owner |  |  |  |
| On-call responder |  |  |  |
| Database/operator owner |  |  |  |
| Security reviewer |  |  |  |
| Product/stakeholder approver |  |  |  |

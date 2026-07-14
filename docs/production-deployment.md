# Production Deployment

QuickBite ships a provider-neutral Helm baseline in `infra/helm/quickbite`. The chart deploys the web app, YARP gateway, and service APIs into isolated Kubernetes namespaces with internal service discovery, ingress, TLS hooks, probes, resource controls, and autoscaling.

## Topology

- `quickbite-web` serves the static frontend through Nginx.
- `quickbite-gateway` is the only public API entry point. Ingress routes `/identity`, `/catalog`, `/orders`, `/payments`, and `/delivery` to the gateway.
- Identity, Catalog, Orders, Payments, and Delivery APIs are `ClusterIP` services reachable only inside the namespace.
- SQL Server and Kafka are expected to be managed platform services or separately operated cluster services. They are not installed by this chart.
- Prometheus can scrape API and gateway pods through `/metrics`; health probes use `/health/live` and `/health/ready`.

## Environments

Use separate namespaces and values files for staging and production:

```powershell
helm upgrade --install quickbite infra/helm/quickbite `
  --namespace quickbite-staging `
  --create-namespace `
  -f infra/helm/quickbite/values-staging.yaml `
  --set global.imageTag=git-<commit-sha>

helm upgrade --install quickbite infra/helm/quickbite `
  --namespace quickbite-production `
  --create-namespace `
  -f infra/helm/quickbite/values-production.yaml `
  --set global.imageTag=git-<commit-sha>
```

Staging defaults to smaller replica counts and the staging certificate issuer. Production raises API, gateway, and web minimum replicas and uses the production certificate issuer. Both environments should deploy immutable `git-<commit-sha>` image tags produced by the release workflow.

## Secrets And Configuration

Create a Kubernetes secret before installing the chart, or set `secrets.create=true` for a non-production bootstrap. The recommended production secret name is `quickbite-runtime-secrets`.

Required keys:

- `identity-connection-string`
- `catalog-connection-string`
- `orders-connection-string`
- `payments-connection-string`
- `delivery-connection-string`
- `jwt-key`

Example:

```powershell
kubectl create secret generic quickbite-runtime-secrets `
  --namespace quickbite-staging `
  --from-literal=identity-connection-string="<managed-sql-identity-connection>" `
  --from-literal=catalog-connection-string="<managed-sql-catalog-connection>" `
  --from-literal=orders-connection-string="<managed-sql-orders-connection>" `
  --from-literal=payments-connection-string="<managed-sql-payments-connection>" `
  --from-literal=delivery-connection-string="<managed-sql-delivery-connection>" `
  --from-literal=jwt-key="<strong-signing-key>"
```

Kafka bootstrap servers, topic partition count, replication factor, ingress host, TLS secret, image registry, and image tag are configured through Helm values. Keep provider-specific identity, secret-store CSI, External Secrets, or cloud Key Vault wiring outside this baseline until the hosting provider is chosen.

## Ingress, DNS, And TLS

The chart creates one `networking.k8s.io/v1` Ingress. Configure DNS so the environment host points at the ingress controller load balancer:

- staging: `staging.quickbite.example.com`
- production: `quickbite.example.com`

TLS termination is owned by the ingress layer. The default annotations assume cert-manager with separate staging and production cluster issuers. If the platform uses managed certificates instead, replace the annotations and keep the same host/path routing model.

## Readiness, Liveness, And Rollouts

API and gateway pods expose:

- `/health/live` for process liveness
- `/health/ready` for dependency readiness
- `/metrics` for Prometheus scraping

The web pod uses `/` for liveness and readiness. Deployments use rolling updates with `maxUnavailable: 0` and startup probes on .NET workloads to avoid routing traffic before startup checks settle.

## Scaling And Resources

Each component defines CPU and memory requests/limits plus an HPA using CPU utilization. Production values raise minimum and maximum replica counts for the gateway and business-critical APIs. The cluster must provide Metrics Server for HPA support.

Recommended future provider-specific triggers:

- Gateway: request rate, p95 latency, and HTTP 5xx ratio.
- Orders, Payments, Delivery: Kafka consumer lag and outbox publish backlog.
- Catalog: request rate and database read latency.
- Identity: login rate, token refresh rate, and HTTP 401/429 trends.

## Migration And Release Order

Run migration bundles from the release workflow before deploying the matching service images:

1. Identity
2. Catalog
3. Orders
4. Payments
5. Delivery

Deploy with immutable image tags and keep the previous known-good tag set for rollback. See `docs/release-management.md` for release packaging and migration bundle details.

## Frontend Build Note

The current Vite frontend bakes `VITE_API_BASE_URL` at build time. Build or promote the web image with the correct environment API base URL. The ingress topology supports same-domain API paths, so production images should use the public environment origin such as `https://quickbite.example.com`.

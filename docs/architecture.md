# Architecture Overview

QuickBite uses a modular monorepo with separate projects for each microservice layer:

- `Api`: HTTP surface, request validation, startup configuration.
- `Application`: contracts and use-case facing abstractions.
- `Domain`: entities and enums.
- `Infrastructure`: EF Core, Kafka integration, service implementations.

## Service responsibilities

- Identity: account registration, login, JWT issuing skeleton.
- Catalog: read-only restaurant and menu access with seeded demo data.
- Orders: order persistence and `OrderCreatedEvent` publishing.
- Payments: payment persistence and simulated asynchronous handling.
- Delivery: courier assignment and delivery creation after successful payment.
- Gateway: reverse proxy entry point for the frontend.

## Shared building blocks

- `QuickBite.BuildingBlocks.Common`: base entity and simple result primitives.
- `QuickBite.BuildingBlocks.Contracts`: integration-event contracts, version constants, and event envelope.
- `QuickBite.BuildingBlocks.Kafka`: producer abstraction, hosted consumer base, typed options, topic initialization, retry, and dead-letter support.
- `QuickBite.BuildingBlocks.Observability`: Serilog bootstrapping and correlation id middleware.

## Data ownership

QuickBite follows a database-per-service approach while using a single SQL Server instance in local development:

- `QuickBiteIdentityDb`
- `QuickBiteCatalogDb`
- `QuickBiteOrdersDb`
- `QuickBitePaymentsDb`
- `QuickBiteDeliveryDb`

Each service owns its own DbContext and schema boundary. Cross-service coordination flows through Kafka contracts instead of direct database access.

Database creation now follows EF Core migrations instead of `EnsureCreated()`. Development environments can apply migrations automatically and seed demo/reference data through the `DatabaseInitialization` configuration section.

See `docs/database-architecture.md` for the implemented tables, seed policy, and migration workflow.

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
- `QuickBite.BuildingBlocks.Contracts`: integration-event contracts and envelope.
- `QuickBite.BuildingBlocks.Kafka`: producer abstraction, hosted consumer base, typed options.
- `QuickBite.BuildingBlocks.Observability`: Serilog bootstrapping and correlation id middleware.

## Data ownership

QuickBite follows a database-per-service approach while using a single SQL Server instance in local development:

- `QuickBiteIdentityDb`
- `QuickBiteCatalogDb`
- `QuickBiteOrdersDb`
- `QuickBitePaymentsDb`
- `QuickBiteDeliveryDb`

Each service owns its own DbContext and schema boundary. Cross-service coordination flows through Kafka contracts instead of direct database access.

# QuickBite

QuickBite is an event-driven food delivery starter platform built as a .NET 8 monorepo. The repository contains five backend microservices, a YARP gateway, a React frontend shell, shared building blocks, Docker Compose infrastructure, checked-in EF Core migrations, and test-project scaffolding so development can continue from a clean professional baseline.

## What is included

- Identity service with registration, login, and JWT token generation skeleton.
- Catalog service with seeded restaurants and menu items.
- Orders service with order creation and `OrderCreated` Kafka publishing.
- Payments service with `OrderCreated` consumption and simulated payment handling.
- Delivery service with payment-success consumption and delivery assignment.
- YARP gateway with service routing.
- React + TypeScript frontend wired to the gateway.
- SQL Server + Kafka + Kafka UI orchestration with Docker Compose.
- Database-per-service migrations and environment-aware seed strategy.
- Versioned Kafka topic configuration with retry and dead-letter topic support.

## Solution structure

```text
/
  QuickBite.sln
  docker-compose.yml
  docs/
  src/
    BuildingBlocks/
    Services/
    Gateway/
  frontend/
    quickbite-web/
  tests/
```

## Services

- `QuickBite.Identity.Api`: `POST /api/auth/register`, `POST /api/auth/login`
- `QuickBite.Catalog.Api`: `GET /api/restaurants`, `GET /api/restaurants/{id}`, `GET /api/restaurants/{id}/menu`
- `QuickBite.Orders.Api`: `POST /api/orders`, `GET /api/orders/{id}`
- `QuickBite.Payments.Api`: `GET /api/payments/{orderId}`
- `QuickBite.Delivery.Api`: `GET /api/deliveries/{orderId}`
- `QuickBite.Gateway`: `/identity/*`, `/catalog/*`, `/orders/*`, `/payments/*`, `/delivery/*`

All services expose `GET /health`.

## Tech stack

- Backend: .NET 8, ASP.NET Core Web API, EF Core, SQL Server, Kafka, Serilog, FluentValidation, YARP
- Frontend: React, TypeScript, Vite, React Router, TanStack Query, Axios
- Infrastructure: Docker Compose, SQL Server, Kafka, Zookeeper, Kafka UI
- Testing: xUnit project skeletons per service

## Running locally

### Local runtime modes

QuickBite now supports two recommended local run modes plus a Windows host-mode fallback.

#### Mode A: infrastructure only

Use this mode for the fastest development loop. SQL Server and Kafka run in Docker, while the APIs, gateway, and frontend run from the host machine.

1. Copy `.env.example` to `.env` and adjust values if needed.
2. Start shared infrastructure:

   `docker compose up -d`

3. Start backend projects from the host:

   - `dotnet run --project src/Services/Identity/QuickBite.Identity.Api`
   - `dotnet run --project src/Services/Catalog/QuickBite.Catalog.Api`
   - `dotnet run --project src/Services/Orders/QuickBite.Orders.Api`
   - `dotnet run --project src/Services/Payments/QuickBite.Payments.Api`
   - `dotnet run --project src/Services/Delivery/QuickBite.Delivery.Api`
   - `dotnet run --project src/Gateway/QuickBite.Gateway`

4. Start the frontend from the host:

   - `cd frontend/quickbite-web`
   - `npm install`
   - `npm run dev`

Before using EF Core CLI commands locally:

- `dotnet tool restore`

#### Mode A2: Windows host mode without Docker

Use this mode when Docker is unavailable on a Windows machine. It uses LocalDB for the service databases, disables Kafka in `Development`, and starts the APIs plus gateway on the fixed localhost ports expected by the frontend.

1. Ensure `sqllocaldb` is available.
2. Start everything:

   `powershell -ExecutionPolicy Bypass -File .\scripts\start-local.ps1`

3. Stop everything:

   `powershell -ExecutionPolicy Bypass -File .\scripts\stop-local.ps1`

4. Open:
   - Frontend: `http://localhost:3000`
   - Gateway: `http://localhost:8080`

#### Mode B: full-stack container parity

Use this mode when validating the full containerized experience.

1. Copy `.env.example` to `.env` and adjust values if needed.
2. Start the full stack:

   `docker compose -f docker-compose.yml -f docker-compose.fullstack.yml up --build`

3. Open:
   - Gateway: `http://localhost:8080/health`
   - Gateway readiness: `http://localhost:8080/health/ready`
   - Kafka UI: `http://localhost:8085`
   - Frontend: `http://localhost:3000`

### Backend services from the repo root

1. Restore packages with `dotnet restore QuickBite.sln`.
2. Restore the EF Core local tool with `dotnet tool restore`.
3. Start SQL Server and Kafka locally or with `docker compose up -d`.
4. On Windows without Docker, you can use `scripts/start-local.ps1` instead of the Docker-backed workflow.
5. Run each API project from `src/Services/*/*Api` and the gateway from `src/Gateway/QuickBite.Gateway`.
6. Development appsettings use fixed localhost ports, apply migrations automatically, and keep Kafka disabled by default for host-mode startup.
7. Every service now exposes:
   - `/health`
   - `/health/live`
   - `/health/ready`

### Frontend

1. Install dependencies in `frontend/quickbite-web` with `npm install`.
2. Copy `frontend/quickbite-web/.env.example` to `.env`.
3. Run `npm run dev`.
4. Run `npm test` for frontend runtime/config tests.

## Event flow

1. The Orders service persists a new order and publishes `order.created` to `quickbite.orders.order-created.v1`.
2. The Payments service consumes the event, simulates payment, stores a payment record, and publishes `payment.succeeded` or `payment.failed`.
3. The Delivery service consumes `payment.succeeded`, creates a delivery, assigns a placeholder courier, and publishes `delivery.assigned`.
4. Consumers retry transient failures and move permanently failed messages to `.dlq` topics.

See `docs/architecture.md`, `docs/event-flow.md`, and `docs/local-development.md` for more detail.
Database details are documented in `docs/database-architecture.md`.
Kafka details are documented in `docs/kafka-architecture.md`.

## Current status

This initial version focuses on architecture, wiring, and developer experience:

- Shared contracts and Kafka abstractions are in place.
- Kafka now uses versioned topics, enriched envelopes, idempotent producers, manual consumer commits, bounded retries, and dead-letter topics.
- Each service has its own DbContext, initial migration, and owned database.
- Identity, Catalog, and Delivery seed development data through startup configuration.
- The frontend is intentionally lightweight but already points at the gateway.
- Local development now supports both infra-only and full-stack containerized workflows.
- The repository includes a local `dotnet-ef` tool manifest for database work.

## Next steps

- Add real authentication and authorization flows to downstream services.
- Introduce outbox and inbox reliability patterns for exactly-once business processing.
- Add richer business workflows and domain validation.
- Add integration tests and deeper service-specific data models.

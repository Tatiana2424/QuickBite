# QuickBite

QuickBite is an event-driven food delivery starter platform built as a .NET 8 monorepo. The repository contains five backend microservices, a YARP gateway, a React frontend shell, shared building blocks, Docker Compose infrastructure, and test-project scaffolding so development can continue from a clean professional baseline.

## What is included

- Identity service with registration, login, and JWT token generation skeleton.
- Catalog service with seeded restaurants and menu items.
- Orders service with order creation and `OrderCreated` Kafka publishing.
- Payments service with `OrderCreated` consumption and simulated payment handling.
- Delivery service with payment-success consumption and delivery assignment.
- YARP gateway with service routing.
- React + TypeScript frontend wired to the gateway.
- SQL Server + Kafka + Kafka UI orchestration with Docker Compose.

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

### Docker Compose

1. Copy `.env.example` to `.env` and adjust values if needed.
2. Run `docker compose up --build`.
3. Open:
   - Gateway: `http://localhost:8080/health`
   - Kafka UI: `http://localhost:8085`
   - Frontend: `http://localhost:3000`

### Backend services from the repo root

1. Restore packages with `dotnet restore QuickBite.sln`.
2. Start SQL Server and Kafka locally or with Compose.
3. Run each API project from `src/Services/*/*Api` and the gateway from `src/Gateway/QuickBite.Gateway`.
4. Development appsettings target `localhost` for SQL Server and Kafka.

### Frontend

1. Install dependencies in `frontend/quickbite-web` with `npm install`.
2. Copy `frontend/quickbite-web/.env.example` to `.env`.
3. Run `npm run dev`.

## Event flow

1. The Orders service persists a new order and publishes `order.created`.
2. The Payments service consumes the event, simulates payment, stores a payment record, and publishes `payment.succeeded` or `payment.failed`.
3. The Delivery service consumes `payment.succeeded`, creates a delivery, assigns a placeholder courier, and publishes `delivery.assigned`.

See `docs/architecture.md`, `docs/event-flow.md`, and `docs/local-development.md` for more detail.

## Current status

This initial version focuses on architecture, wiring, and developer experience:

- Shared contracts and Kafka abstractions are in place.
- Each service has its own DbContext and database.
- Catalog and Delivery seed development data.
- The frontend is intentionally lightweight but already points at the gateway.

## Next steps

- Add real authentication and authorization flows to downstream services.
- Introduce outbox and inbox reliability patterns.
- Add richer business workflows and domain validation.
- Add integration tests and per-service migrations.

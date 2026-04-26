# Database Architecture

## Overview

QuickBite uses a database-per-service model. Each microservice owns its own schema, migration history, and seed strategy even when the databases live on the same SQL Server instance during local development.

Current logical databases:

- `QuickBiteIdentityDb`
- `QuickBiteCatalogDb`
- `QuickBiteOrdersDb`
- `QuickBitePaymentsDb`
- `QuickBiteDeliveryDb`

This keeps service boundaries explicit and prevents accidental coupling through shared tables.

## Environment strategy

- Local Docker mode: SQL Server container with one logical database per service.
- Windows host mode: LocalDB with one logical database per service.
- Full-stack Docker mode: SQL Server container plus automatic migration application on startup.
- Non-development environments: migrations remain part of the repository, but startup migration execution is disabled by default in configuration.

## Implemented schemas

### Identity

Implemented tables:

- `Users`
- `Roles`
- `UserRoles`
- `RefreshTokens`

Notes:

- `Users.Email` is unique.
- `Roles.Name` is unique.
- `UserRoles` enforces unique `UserId + RoleId`.
- Default roles are seeded on startup.
- Demo users are seeded only when `DatabaseInitialization:SeedDemoData` is enabled.

### Catalog

Implemented tables:

- `Restaurants`
- `MenuItems`

Notes:

- `MenuItems.RestaurantId` points to `Restaurants`.
- Demo restaurants and menu items are seeded only in demo-data environments.

### Orders

Implemented tables:

- `Orders`
- `OrderItems`
- `OrderStatusHistory`
- `OutboxMessages`
- `InboxMessages`

Notes:

- `OrderItems.OrderId` points to `Orders`.
- `Orders.UserId + Orders.IdempotencyKey` is unique when an idempotency key is supplied.
- Order state remains inside the service database and is projected outward through API responses and events.
- `OrderStatusHistory` records saga state changes such as payment processing, confirmed, or failed.
- `OutboxMessages` stores outgoing `order.created` events transactionally with the order.
- `InboxMessages` stores processed payment-result events so duplicate deliveries do not corrupt order state.

### Payments

Implemented tables:

- `Payments`
- `PaymentStatusHistory`
- `OutboxMessages`
- `InboxMessages`

Notes:

- `Payments.OrderId` is unique to guarantee one payment aggregate per order in the current starter workflow.
- `PaymentStatusHistory` records simulated provider outcomes.
- `InboxMessages` stores processed `order.created` events.
- `OutboxMessages` stores outgoing `payment.succeeded` and `payment.failed` events transactionally with payment records.

### Delivery

Implemented tables:

- `Couriers`
- `Deliveries`
- `DeliveryStatusHistory`
- `OutboxMessages`
- `InboxMessages`

Notes:

- `Deliveries.OrderId` is unique.
- Demo couriers are seeded only in demo-data environments.
- `DeliveryStatusHistory` records assignment state transitions.
- `InboxMessages` stores processed `payment.succeeded` events.
- `OutboxMessages` stores outgoing `delivery.assigned` events transactionally with delivery records.

## Planned next-step tables

These are intentionally not implemented yet, but the structure now leaves room for them:

- Identity: `UserAddresses`, richer session management, audit records
- Catalog: menu categories, availability windows, restaurant status snapshots
- Orders: delivery address snapshot, cancellation/refund state, richer reconciliation records
- Payments: payment attempts, provider responses, refund records
- Delivery: assignment history, courier availability windows, route checkpoints

## Migrations and seeding

Each service now has:

- an EF Core initial migration checked into source control
- a design-time `DbContext` factory for consistent migration generation
- startup configuration under `DatabaseInitialization`

Key flags:

- `ApplyMigrationsOnStartup`
- `SeedDemoData`
- `RecreateDatabaseIfMigrationHistoryMissing`

In development, the app can automatically recreate legacy pre-migration local databases that were previously created through `EnsureCreated()`. This is only intended for local/demo environments.

## Tooling

The repository includes a local `dotnet-ef` tool manifest.

Typical commands:

```powershell
dotnet tool restore
dotnet dotnet-ef migrations list --project src/Services/Identity/QuickBite.Identity.Infrastructure/QuickBite.Identity.Infrastructure.csproj
dotnet dotnet-ef database update --project src/Services/Identity/QuickBite.Identity.Infrastructure/QuickBite.Identity.Infrastructure.csproj
```

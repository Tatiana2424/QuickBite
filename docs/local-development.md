# Local Development

## Prerequisites

- .NET SDK 8 compatible environment
- Node.js 20+ and npm
- Docker Desktop or another Docker engine

## Backend

1. Restore the solution from the repository root:

   `dotnet restore QuickBite.sln`

2. Start SQL Server and Kafka locally. Docker Compose is the easiest option.
3. Run backend services individually if needed:

   - `dotnet run --project src/Services/Identity/QuickBite.Identity.Api`
   - `dotnet run --project src/Services/Catalog/QuickBite.Catalog.Api`
   - `dotnet run --project src/Services/Orders/QuickBite.Orders.Api`
   - `dotnet run --project src/Services/Payments/QuickBite.Payments.Api`
   - `dotnet run --project src/Services/Delivery/QuickBite.Delivery.Api`
   - `dotnet run --project src/Gateway/QuickBite.Gateway`

The service startup path uses `Database.EnsureCreated()` for the initial developer experience.

## Frontend

1. `cd frontend/quickbite-web`
2. `npm install`
3. `npm run dev`

The frontend points to `VITE_API_BASE_URL`, which defaults to `http://localhost:8080`.

## Docker Compose

1. Copy `.env.example` to `.env`.
2. Run `docker compose up --build`.
3. Confirm health and route behavior through the gateway.

## Troubleshooting

- If SQL Server starts slowly, restart the APIs after the database container becomes healthy.
- If Kafka consumers appear idle, check topic creation and broker reachability through Kafka UI.
- If the frontend cannot reach APIs, verify the gateway is reachable on port `8080`.

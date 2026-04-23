# Local Development

## Prerequisites

- .NET SDK 8 compatible environment
- Node.js 20+ and npm
- Docker Desktop or another Docker engine for the Docker-backed workflows
- Windows LocalDB if you want to use the no-Docker host fallback

## Local runtime modes

QuickBite should be developed in one of two local modes depending on the task.

### Mode A: infrastructure only

Use this mode for everyday development. Shared infrastructure runs in Docker, while the APIs, gateway, and frontend run from the host.

1. Restore the solution:

   `dotnet restore QuickBite.sln`

2. Copy `.env.example` to `.env`.
3. Start infrastructure only:

   `docker compose up -d`

4. Run the backend services you need:

   - `dotnet run --project src/Services/Identity/QuickBite.Identity.Api`
   - `dotnet run --project src/Services/Catalog/QuickBite.Catalog.Api`
   - `dotnet run --project src/Services/Orders/QuickBite.Orders.Api`
   - `dotnet run --project src/Services/Payments/QuickBite.Payments.Api`
   - `dotnet run --project src/Services/Delivery/QuickBite.Delivery.Api`
   - `dotnet run --project src/Gateway/QuickBite.Gateway`

5. Use the development appsettings that target:

   - SQL Server on `localhost:1433`
   - Kafka on `localhost:9092`

### Mode A2: Windows host mode without Docker

Use this mode when Docker is unavailable. It is a Windows-only fallback that uses LocalDB for persistence and keeps Kafka disabled in `Development` so the full UI stack can still start.

1. Start the stack:

   `powershell -ExecutionPolicy Bypass -File .\scripts\start-local.ps1`

2. Open:

   - frontend: `http://localhost:3000`
   - gateway: `http://localhost:8080`

3. Stop the stack:

   `powershell -ExecutionPolicy Bypass -File .\scripts\stop-local.ps1`

4. Expected local ports:

   - Identity: `5001`
   - Catalog: `5002`
   - Orders: `5003`
   - Payments: `5004`
   - Delivery: `5005`
   - Gateway: `8080`
   - Frontend: `3000`

### Mode B: full-stack container parity

Use this mode when validating image builds, container networking, and startup flow.

1. Copy `.env.example` to `.env`.
2. Start the full stack:

   `docker compose -f docker-compose.yml -f docker-compose.fullstack.yml up --build`

3. Confirm these endpoints:

   - gateway health: `http://localhost:8080/health`
   - gateway readiness: `http://localhost:8080/health/ready`
   - Kafka UI: `http://localhost:8085`
   - frontend: `http://localhost:3000`

## Backend

- All backend services now fail fast when required runtime configuration is missing.
- Host-mode development now uses fixed localhost ports that match the gateway routes.
- Runtime health endpoints are exposed on:
  - `/health`
  - `/health/live`
  - `/health/ready`

## Frontend

1. `cd frontend/quickbite-web`
2. `npm install`
3. `npm run dev`
4. `npm test`

The frontend validates `VITE_API_BASE_URL` and defaults to `http://localhost:8080`.

## Docker Compose

1. Copy `.env.example` to `.env`.
2. For infrastructure only, run:

   `docker compose up -d`

3. For full stack parity, run:

   `docker compose -f docker-compose.yml -f docker-compose.fullstack.yml up --build`

## Troubleshooting

- If startup fails immediately, check missing environment variables or invalid `.env` values first. Services now validate required runtime configuration on startup.
- If you are using Windows host mode, make sure `sqllocaldb` is installed and `scripts/start-local.ps1` is running the services with the LocalDB connection strings.
- If SQL Server starts slowly, restart the APIs after the database container becomes healthy.
- If Kafka consumers appear idle in the Docker-backed modes, check topic creation and broker reachability through Kafka UI.
- If the frontend cannot reach APIs, verify the gateway is reachable on port `8080`.

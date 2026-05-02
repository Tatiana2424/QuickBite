# QuickBite Testing Strategy

QuickBite uses automated quality gates to keep the microservices starter safe to extend. The goal is practical confidence: fast checks for most changes, contract tests for cross-service boundaries, and compose validation for local/runtime wiring.

## Test Layers

- Unit tests protect domain behavior, validators, configuration guards, and reusable building blocks.
- Contract tests protect gateway routes, public API route attributes, Kafka topic names, integration event names, and the order-payment-delivery correlation chain.
- Frontend Vitest smoke tests protect the React shell, routing, auth storage, API error handling, and gateway service integration seams.
- Playwright E2E smoke tests protect browser-level catalog loading and the protected login-to-orders journey.
- Docker Compose configuration validation protects service names, networks, environment wiring, and local orchestration syntax.
- Future integration tests should exercise APIs with SQL Server and Kafka through Docker Compose once the workflows are stable enough to run repeatably in CI.

## Local Commands

Run backend checks from the repository root:

```powershell
dotnet restore QuickBite.sln
dotnet build QuickBite.sln
dotnet test QuickBite.sln --collect:"XPlat Code Coverage"
```

Run frontend checks:

```powershell
cd frontend/quickbite-web
npm ci
npm test -- --run
npm run test:e2e
npm run build
```

Validate orchestration without starting the full stack:

```powershell
docker compose -f docker-compose.yml -f docker-compose.fullstack.yml config
```

## CI Quality Gate

The GitHub Actions workflow in `.github/workflows/quality-gates.yml` runs on pull requests to `main` and pushes to `main`. It currently enforces:

- .NET restore, build, and xUnit test execution with XPlat Code Coverage collection.
- Frontend dependency restore, Vitest execution, Playwright E2E smoke tests, and production build.
- Docker Compose configuration validation for the infrastructure-only and full-stack compose files.

## Coverage Policy

Coverage collection is enabled at the CI level. This issue intentionally avoids a hard percentage threshold because the project is still a starter and several integration seams are being introduced. A later issue should add a minimum threshold after API integration tests exist, starting with changed-code coverage before enforcing repository-wide coverage.

## Integration Test Direction

For this pet project, the recommended next step is Compose-based integration testing because QuickBite already has compose files that developers use locally. Testcontainers remains a good future option for isolated service-level tests, but it adds more moving parts on Windows and should be introduced after the compose path is dependable.

## Red-First Workflow

For new behavior, write or update the smallest failing test first. For infrastructure and contract work, prefer executable contract tests that fail when configuration, routes, events, or orchestration drift from the expected architecture.

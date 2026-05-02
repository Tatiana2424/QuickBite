# Frontend Production Readiness

The QuickBite frontend is still intentionally small, but it now has the core production-facing foundation expected from a microservices web client.

## Runtime Configuration

The app validates Vite environment values at startup:

- `VITE_API_BASE_URL`: absolute URL for the gateway. Defaults to `http://localhost:8080`.
- `VITE_APP_ENV`: one of `local`, `development`, `staging`, or `production`.
- `VITE_ENABLE_MONITORING`: explicit `true` or `false`. Defaults to enabled only in production.

Invalid values fail fast during startup instead of creating confusing runtime behavior.

## Authentication

Authentication is handled by `AuthProvider` with a deliberately lightweight React Context model:

- Login calls `POST /identity/api/auth/login` through the gateway.
- Access tokens are attached to gateway requests with an Axios request interceptor.
- Access tokens are refreshed using `POST /identity/api/auth/refresh` before expiry.
- `401` and `403` responses clear the local session and return the user to sign-in.
- Logout calls `POST /identity/api/auth/logout` and clears local session state.

The current starter stores tokens in `localStorage` because the backend exposes token responses directly. If QuickBite later moves to browser-cookie auth, replace the storage implementation without changing page-level auth usage.

## User Experience

Pages now share reusable loading, empty, and error-state components. Gateway and service errors are normalized into `ApiError` so users see consistent messages and trace IDs when the API provides them.

Protected routes should be used for flows that require identity. Public catalog browsing remains unauthenticated, while order lookup is guarded.

## Monitoring

The frontend includes a small monitoring shim and React error boundary:

- Global `error` and `unhandledrejection` listeners report runtime failures.
- React render failures are caught by `ErrorBoundary`.
- Local and development environments log warnings without pretending to send telemetry.
- Production can enable reporting with `VITE_ENABLE_MONITORING=true`.

A future production deployment can replace `reportFrontendError` internals with Sentry, Azure Monitor, OpenTelemetry browser instrumentation, or another provider.

## Testing

Frontend quality gates include:

- Vitest tests for environment validation, API error mapping, auth storage, and app route behavior.
- Playwright E2E smoke tests for catalog loading and protected login-to-orders navigation.
- CI execution for unit/smoke tests, Playwright E2E, and production build.

Local E2E commands:

```powershell
cd frontend/quickbite-web
npm ci
npx playwright install chromium
npm run test:e2e
```

On this Windows development machine, Playwright is configured to use installed Microsoft Edge locally and CI-installed Chromium in GitHub Actions.

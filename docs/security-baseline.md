# Security Baseline

QuickBite is still a pet project, but the codebase now has a practical security baseline that can grow without rewriting the identity model.

## Authentication

Identity owns local username/password authentication for now.

- `POST /api/auth/register` creates a customer account.
- `POST /api/auth/login` returns an access token and refresh token.
- `POST /api/auth/refresh` rotates an active refresh token and returns a new token pair.
- `POST /api/auth/logout` revokes the supplied refresh token.

Access tokens are JWTs with role claims. Refresh tokens are opaque random values. Only refresh-token hashes are stored in SQL Server.

## Refresh token rotation and revocation

Refresh tokens are single-use for renewal:

- Active refresh tokens can be exchanged for a new access token and a new refresh token.
- The used refresh token is revoked during refresh.
- `ReplacedByTokenHash` records the replacement token hash for audit/debugging.
- Expired or revoked refresh tokens cannot be used again.
- Logout revokes the supplied refresh token and returns `204 No Content`.

Future work should add refresh-token family reuse detection if stolen-token scenarios become in scope.

## Roles and policies

Seeded platform roles:

- `Customer`
- `RestaurantAdmin`
- `Courier`
- `PlatformAdmin`

Identity API registers authorization policies for these role groups:

- `Customers`
- `RestaurantAdmins`
- `Couriers`
- `PlatformAdmins`

The current business APIs are not locked down yet because frontend auth flows are still being shaped. When enforcement begins, prefer policy names over hard-coded role checks in controllers.

## Secret handling

`Jwt:Key` is intentionally empty in base `appsettings.json`. Runtime environments must provide it through environment variables, user secrets, Docker Compose, or a real secret store.

Local development may opt into the demo signing key by setting:

`Jwt:AllowDevelopmentSigningKey=true`

Do not enable that setting outside local development.

Recommended future staging/production secret stores:

- Azure Key Vault if hosted on Azure.
- AWS Secrets Manager or Parameter Store if hosted on AWS.
- Docker/Kubernetes secrets if using container orchestration.

## Gateway security baseline

The gateway applies:

- CORS allow-list from `GatewaySecurity:AllowedOrigins`.
- Global fixed-window rate limiting.
- `X-Content-Type-Options: nosniff`.
- `X-Frame-Options: DENY`.
- `Referrer-Policy: no-referrer`.
- restrictive Content Security Policy for gateway responses.
- `Permissions-Policy` disabling camera, microphone, and geolocation.

Retries remain intentionally route-specific rather than global because command routes are not safely retryable by default.

## Service-to-service trust model

Current local development trust is network-bound through Docker Compose service DNS and gateway routing.

Recommended next steps:

- Public clients call only the gateway.
- Internal services should prefer Kafka events for cross-service workflows.
- If synchronous service-to-service HTTP is introduced, use internal JWTs with a platform issuer and short lifetimes.
- For stronger environments, add mTLS between gateway and services or rely on mesh/orchestrator identity.
- Never share service databases as an authorization shortcut.

## External identity provider question

QuickBite can continue owning auth for the pet-project phase. If the platform grows, evaluate external providers such as Auth0, Azure AD B2C, Amazon Cognito, or Keycloak before implementing advanced account recovery, MFA, device management, and social login in-house.

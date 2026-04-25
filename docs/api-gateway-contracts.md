# API and Gateway Contracts

This document defines the starter HTTP conventions for QuickBite services and the gateway.

## Public entry point

The frontend should call the YARP gateway instead of individual services:

- `/identity/*` forwards to Identity.
- `/catalog/*` forwards to Catalog.
- `/orders/*` forwards to Orders.
- `/payments/*` forwards to Payments.
- `/delivery/*` forwards to Delivery.

The gateway strips the public service prefix and forwards to each service's internal `/api/*` routes.

## API versioning policy

The starter API is version `1`. Every API and gateway response includes:

- `X-QuickBite-Api-Version: 1`

Routes are not URL-versioned yet because the project is still in the early API-shaping stage. When breaking HTTP changes are needed, add explicit `/v2` gateway routes rather than changing existing `/api/*` behavior silently.

## Correlation policy

All services and the gateway use `X-Correlation-Id`.

- If the client sends the header, the same value is preserved.
- If the client does not send it, the first service creates one.
- The value is written to the response and pushed into structured logs.
- The gateway forwards the request header to downstream services.

## Error response policy

APIs return `application/problem+json`-compatible Problem Details for validation and known errors. The shared shape includes:

- `type`
- `title`
- `status`
- `detail`
- `instance`
- `traceId`
- `correlationId`
- `apiVersion`
- `path`

Validation failures use `ValidationProblemDetails` and include field-level `errors`.

## Gateway reliability policy

Gateway routes and clusters are validated at startup. Startup fails if:

- no routes are configured;
- no clusters are configured;
- a route references a missing cluster;
- a route path does not start with `/`;
- a cluster destination address is not an absolute HTTP(S) URI;
- a cluster does not define a positive `HttpRequest:ActivityTimeout`.

Current downstream timeout defaults:

- Identity: 15 seconds
- Catalog: 15 seconds
- Orders: 20 seconds
- Payments: 15 seconds
- Delivery: 15 seconds

Retries are intentionally not enabled globally. They are safe for idempotent reads but risky for commands such as order creation and registration. Add route-specific retry policy later only for read-only routes with clear idempotency guarantees.

## Internal endpoint exposure

For local development, individual service ports remain available. For application usage, the gateway is the stable entry point. Service-specific ports should be treated as internal/debug endpoints, not frontend integration targets.

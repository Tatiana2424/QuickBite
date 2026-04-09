# Event Flow

## Order lifecycle

1. A client creates an order through the Orders API.
2. The Orders service stores the order and publishes `order.created`.
3. The Payments service consumes `order.created` and decides whether the simulated payment succeeds.
4. A successful payment emits `payment.succeeded`; a failure emits `payment.failed`.
5. The Delivery service consumes `payment.succeeded`, creates a delivery, assigns a courier, and emits `delivery.assigned`.

## Topics used in the starter

- `quickbite.order.created`
- `quickbite.payment.succeeded`
- `quickbite.payment.failed`
- `quickbite.delivery.assigned`
- `quickbite.delivery.completed`

## Scope of the first version

The current implementation intentionally keeps messaging simple:

- direct publish after persistence
- development-focused auto topic creation
- hosted consumer loops per service
- no outbox, retries, or saga coordination yet

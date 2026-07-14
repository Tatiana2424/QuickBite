import { useQuery } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorState, LoadingState } from "../components/AsyncState";
import type { Delivery, Order, Payment } from "../models";
import { getDeliveryForOrder, getOrder, getPaymentForOrder } from "../services/quickbiteService";

const activeOrderStatuses = new Set(["Created", "PaymentProcessing", "Confirmed"]);
const activePaymentStatuses = new Set(["Pending", "Processing"]);
const activeDeliveryStatuses = new Set(["Pending", "Assigned", "PickedUp"]);

export function OrderDetailsPage() {
  const { orderId = "" } = useParams();
  const orderQuery = useQuery({
    queryKey: ["order", orderId],
    queryFn: () => getOrder(orderId),
    enabled: Boolean(orderId),
    refetchInterval: (query) => shouldPollOrder(query.state.data) ? 5_000 : false
  });
  const paymentQuery = useQuery({
    queryKey: ["payment", orderId],
    queryFn: () => getPaymentForOrder(orderId),
    enabled: Boolean(orderId) && orderQuery.isSuccess,
    refetchInterval: (query) => shouldPollPayment(query.state.data) ? 5_000 : false
  });
  const deliveryQuery = useQuery({
    queryKey: ["delivery", orderId],
    queryFn: () => getDeliveryForOrder(orderId),
    enabled: Boolean(orderId) && orderQuery.isSuccess,
    refetchInterval: (query) => shouldPollDelivery(query.state.data) ? 5_000 : false
  });

  if (orderQuery.isLoading) {
    return <LoadingState label="Loading order..." />;
  }

  if (orderQuery.isError || !orderQuery.data) {
    return <ErrorState error={orderQuery.error} action={<Link className="text-link" to="/orders">Back to orders</Link>} />;
  }

  const order = orderQuery.data;
  const isRefreshing = orderQuery.isFetching || paymentQuery.isFetching || deliveryQuery.isFetching;

  return (
    <section className="stack">
      <div className="order-detail-heading">
        <div>
          <p className="eyebrow">Order details</p>
          <h2>Order {order.id.slice(0, 8)}</h2>
          <p className="muted">{formatOrderDate(order.createdAtUtc)}</p>
        </div>
        <button
          type="button"
          className="button-secondary"
          disabled={isRefreshing}
          onClick={() => {
            void orderQuery.refetch();
            void paymentQuery.refetch();
            void deliveryQuery.refetch();
          }}
        >
          {isRefreshing ? "Refreshing..." : "Refresh status"}
        </button>
      </div>
      <OrderProgressTimeline order={order} payment={paymentQuery.data} delivery={deliveryQuery.data} />
      <div className="lifecycle-grid">
        <LifecycleCard title="Order" status={order.status}>
          <p>Total: ${order.totalAmount.toFixed(2)}</p>
          <p className="muted">{describeOrderStatus(order.status)}</p>
        </LifecycleCard>
        <PaymentLifecycleCard payment={paymentQuery.data} isLoading={paymentQuery.isLoading} error={paymentQuery.error} />
        <DeliveryLifecycleCard delivery={deliveryQuery.data} isLoading={deliveryQuery.isLoading} error={deliveryQuery.error} />
      </div>
      <article className="panel order-detail">
        <div className="order-card__header">
          <strong>Items</strong>
          <span>${order.totalAmount.toFixed(2)}</span>
        </div>
        <ul className="order-items">
          {order.items.map((item) => (
            <li key={`${item.menuItemId}-${item.name}`}>
              <span>{item.quantity} x {item.name}</span>
              <strong>${(item.quantity * item.unitPrice).toFixed(2)}</strong>
            </li>
          ))}
        </ul>
      </article>
      <article className="panel order-detail">
        <div>
          <p className="eyebrow">Delivery address</p>
          <strong>{order.deliveryAddress.line1}</strong>
        </div>
        <address className="delivery-address">
          {order.deliveryAddress.line2 && <span>{order.deliveryAddress.line2}</span>}
          <span>{order.deliveryAddress.city}, {order.deliveryAddress.state} {order.deliveryAddress.postalCode}</span>
          <span>{order.deliveryAddress.country}</span>
        </address>
      </article>
      <Link className="text-link" to="/orders">Back to orders</Link>
    </section>
  );
}

function PaymentLifecycleCard({
  payment,
  isLoading,
  error
}: {
  payment: Payment | null | undefined;
  isLoading: boolean;
  error: unknown;
}) {
  if (isLoading) {
    return <LifecycleCard title="Payment" status="Loading">Checking payment status. This can take a few seconds after checkout.</LifecycleCard>;
  }

  if (error) {
    return <LifecycleCard title="Payment" status="Unavailable">Payment status could not be loaded. Use refresh in a moment.</LifecycleCard>;
  }

  if (!payment) {
    return <LifecycleCard title="Payment" status="Pending">Payment is being prepared. You can stay here; this page checks for updates automatically.</LifecycleCard>;
  }

  return (
    <LifecycleCard title="Payment" status={payment.status} tone={payment.status === "Failed" ? "danger" : "success"}>
      <p>Amount: ${payment.amount.toFixed(2)}</p>
      {payment.status === "Succeeded" && <p className="muted">Payment is complete. The restaurant can start preparing the order.</p>}
      {payment.status === "Failed" && (
        <p className="muted">
          {payment.failureReason ?? "Payment failed."} Try placing the order again or use a different payment method when payments are connected.
        </p>
      )}
    </LifecycleCard>
  );
}

function DeliveryLifecycleCard({
  delivery,
  isLoading,
  error
}: {
  delivery: Delivery | null | undefined;
  isLoading: boolean;
  error: unknown;
}) {
  if (isLoading) {
    return <LifecycleCard title="Delivery" status="Loading">Checking delivery status. Courier assignment may happen after payment clears.</LifecycleCard>;
  }

  if (error) {
    return <LifecycleCard title="Delivery" status="Unavailable">Delivery status could not be loaded. Refresh again shortly.</LifecycleCard>;
  }

  if (!delivery) {
    return <LifecycleCard title="Delivery" status="Pending">Delivery will be assigned after payment confirmation. No action is needed from you right now.</LifecycleCard>;
  }

  return (
    <LifecycleCard title="Delivery" status={delivery.status} tone={delivery.status === "Delivered" || delivery.status === "Completed" ? "success" : "info"}>
      <p>{delivery.courierName}</p>
      <p className="muted">{delivery.courierPhoneNumber}</p>
      <p className="muted">{delivery.address.line1}, {delivery.address.city}</p>
    </LifecycleCard>
  );
}

function LifecycleCard({
  title,
  status,
  tone = "info",
  children
}: {
  title: string;
  status: string;
  tone?: "info" | "success" | "danger";
  children: ReactNode;
}) {
  return (
    <article className="panel lifecycle-card">
      <div className="order-card__header">
        <strong>{title}</strong>
        <span className={`status-pill status-pill--${tone}`}>{status}</span>
      </div>
      <div>{children}</div>
    </article>
  );
}

function OrderProgressTimeline({
  order,
  payment,
  delivery
}: {
  order: Order;
  payment: Payment | null | undefined;
  delivery: Delivery | null | undefined;
}) {
  const paymentFailed = payment?.status === "Failed" || order.status === "Failed";
  const steps = [
    {
      label: "Order placed",
      status: "Complete",
      complete: true,
      description: "We received your order and saved the delivery details."
    },
    {
      label: "Payment",
      status: payment?.status ?? (paymentFailed ? "Failed" : "Pending"),
      complete: payment?.status === "Succeeded",
      failed: paymentFailed,
      description: paymentFailed
        ? "Payment did not go through. You can place a new order when ready."
        : payment?.status === "Succeeded"
          ? "Payment succeeded and the kitchen can continue."
          : "Payment is still processing. This usually updates automatically."
    },
    {
      label: "Delivery",
      status: delivery?.status ?? "Pending",
      complete: delivery?.status === "Delivered" || delivery?.status === "Completed",
      disabled: paymentFailed,
      description: paymentFailed
        ? "Delivery is paused because payment failed."
        : delivery
          ? `${delivery.courierName} is assigned to this delivery.`
          : "A courier will be assigned after payment is confirmed."
    }
  ];

  return (
    <section className="panel progress-panel" aria-label="Order progress">
      {steps.map((step) => (
        <article
          key={step.label}
          className={[
            "progress-step",
            step.complete ? "progress-step--complete" : "",
            step.failed ? "progress-step--failed" : "",
            step.disabled ? "progress-step--disabled" : ""
          ].filter(Boolean).join(" ")}
        >
          <span className="progress-dot" aria-hidden="true" />
          <div>
            <div className="progress-step__header">
              <strong>{step.label}</strong>
              <span>{step.status}</span>
            </div>
            <p className="muted">{step.description}</p>
          </div>
        </article>
      ))}
    </section>
  );
}

function describeOrderStatus(status: string) {
  switch (status) {
    case "PaymentProcessing":
      return "Your order is placed and waiting for payment confirmation.";
    case "Confirmed":
      return "Your order is confirmed and moving through preparation and delivery.";
    case "Failed":
      return "This order stopped because payment failed.";
    default:
      return "We are tracking this order and will update the next step here.";
  }
}

function shouldPollOrder(order: Order | undefined) {
  return !order || activeOrderStatuses.has(order.status);
}

function shouldPollPayment(payment: Payment | null | undefined) {
  return !payment || activePaymentStatuses.has(payment.status);
}

function shouldPollDelivery(delivery: Delivery | null | undefined) {
  return !delivery || activeDeliveryStatuses.has(delivery.status);
}

function formatOrderDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "full",
    timeStyle: "short"
  }).format(new Date(value));
}

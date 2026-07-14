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

  return (
    <section className="stack">
      <div>
        <p className="eyebrow">Order details</p>
        <h2>Order {order.id.slice(0, 8)}</h2>
        <p className="muted">{formatOrderDate(order.createdAtUtc)}</p>
      </div>
      <div className="lifecycle-grid">
        <LifecycleCard title="Order" status={order.status}>
          <p>Total: ${order.totalAmount.toFixed(2)}</p>
          <p className="muted">{order.items.length} item{order.items.length === 1 ? "" : "s"}</p>
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
    return <LifecycleCard title="Payment" status="Loading">Checking payment status...</LifecycleCard>;
  }

  if (error) {
    return <LifecycleCard title="Payment" status="Unavailable">Payment status could not be loaded.</LifecycleCard>;
  }

  if (!payment) {
    return <LifecycleCard title="Payment" status="Pending">Payment is being prepared.</LifecycleCard>;
  }

  return (
    <LifecycleCard title="Payment" status={payment.status}>
      <p>Amount: ${payment.amount.toFixed(2)}</p>
      {payment.failureReason && <p className="muted">{payment.failureReason}</p>}
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
    return <LifecycleCard title="Delivery" status="Loading">Checking delivery status...</LifecycleCard>;
  }

  if (error) {
    return <LifecycleCard title="Delivery" status="Unavailable">Delivery status could not be loaded.</LifecycleCard>;
  }

  if (!delivery) {
    return <LifecycleCard title="Delivery" status="Pending">Delivery will be assigned after payment confirmation.</LifecycleCard>;
  }

  return (
    <LifecycleCard title="Delivery" status={delivery.status}>
      <p>{delivery.courierName}</p>
      <p className="muted">{delivery.courierPhoneNumber}</p>
      <p className="muted">{delivery.address.line1}, {delivery.address.city}</p>
    </LifecycleCard>
  );
}

function LifecycleCard({
  title,
  status,
  children
}: {
  title: string;
  status: string;
  children: ReactNode;
}) {
  return (
    <article className="panel lifecycle-card">
      <div className="order-card__header">
        <strong>{title}</strong>
        <span className="status-pill">{status}</span>
      </div>
      <div>{children}</div>
    </article>
  );
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

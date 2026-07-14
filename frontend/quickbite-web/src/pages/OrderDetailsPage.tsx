import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ErrorState, LoadingState } from "../components/AsyncState";
import { getOrder } from "../services/quickbiteService";

export function OrderDetailsPage() {
  const { orderId = "" } = useParams();
  const orderQuery = useQuery({
    queryKey: ["order", orderId],
    queryFn: () => getOrder(orderId),
    enabled: Boolean(orderId)
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
      <article className="panel order-detail">
        <div className="order-card__header">
          <strong>Status: {order.status}</strong>
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
      <Link className="text-link" to="/orders">Back to orders</Link>
    </section>
  );
}

function formatOrderDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "full",
    timeStyle: "short"
  }).format(new Date(value));
}

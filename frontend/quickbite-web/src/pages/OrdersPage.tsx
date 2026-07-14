import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import type { Order } from "../models";
import { getMyOrders } from "../services/quickbiteService";

export function OrdersPage() {
  const ordersQuery = useQuery({
    queryKey: ["my-orders"],
    queryFn: () => getMyOrders()
  });

  if (ordersQuery.isLoading) {
    return <LoadingState label="Loading your orders..." />;
  }

  if (ordersQuery.isError) {
    return <ErrorState error={ordersQuery.error} action={<button type="button" onClick={() => void ordersQuery.refetch()}>Retry</button>} />;
  }

  const orders = ordersQuery.data ?? [];

  return (
    <section className="stack">
      <div>
        <p className="eyebrow">Orders</p>
        <h2>My Orders</h2>
        <p className="muted">Recent orders from your QuickBite account.</p>
      </div>
      {orders.length === 0 ? (
        <EmptyState title="No orders yet">Your completed checkout flow will show recent orders here.</EmptyState>
      ) : (
        <div className="grid">
          {orders.map((order) => (
            <OrderCard key={order.id} order={order} />
          ))}
        </div>
      )}
    </section>
  );
}

function OrderCard({ order }: { order: Order }) {
  const itemSummary = summarizeItems(order);

  return (
    <Link to={`/orders/${order.id}`} className="card order-card" aria-label={`View order ${order.id}`}>
      <div className="order-card__header">
        <strong>{order.status}</strong>
        <span>${order.totalAmount.toFixed(2)}</span>
      </div>
      <p>{itemSummary}</p>
      <small>{formatOrderDate(order.createdAtUtc)}</small>
    </Link>
  );
}

function summarizeItems(order: Order) {
  const visibleItems = order.items.slice(0, 3).map((item) => `${item.quantity} x ${item.name}`);
  const remainingCount = order.items.length - visibleItems.length;

  return remainingCount > 0 ? `${visibleItems.join(", ")} + ${remainingCount} more` : visibleItems.join(", ");
}

function formatOrderDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

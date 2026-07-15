import { useMutation, useQuery } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import { useCart } from "../cart/CartContext";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import type { OrderSummary } from "../models";
import { getMyOrders, getOrder } from "../services/quickbiteService";

export function OrdersPage() {
  const navigate = useNavigate();
  const { replaceItems } = useCart();
  const ordersQuery = useQuery({
    queryKey: ["my-orders"],
    queryFn: () => getMyOrders()
  });
  const reorderMutation = useMutation({
    mutationFn: (orderId: string) => getOrder(orderId),
    onSuccess: (order) => {
      replaceItems(order.items, order.id);
      navigate("/cart", { state: { reorderSource: order.id } });
    }
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
            <OrderCard
              key={order.id}
              order={order}
              isReordering={reorderMutation.isPending && reorderMutation.variables === order.id}
              onReorder={() => reorderMutation.mutate(order.id)}
            />
          ))}
        </div>
      )}
      {reorderMutation.isError && <ErrorState error={reorderMutation.error} />}
    </section>
  );
}

function OrderCard({
  order,
  isReordering,
  onReorder
}: {
  order: OrderSummary;
  isReordering: boolean;
  onReorder: () => void;
}) {
  return (
    <article className="card order-card">
      <div className="order-card__header">
        <strong>{order.status}</strong>
        <span>${order.totalAmount.toFixed(2)}</span>
      </div>
      <p>{order.itemSummary}</p>
      <p className="muted">{order.itemCount} item{order.itemCount === 1 ? "" : "s"}</p>
      <small>{formatOrderDate(order.createdAtUtc)}</small>
      <div className="order-card__actions">
        <Link className="text-link" to={`/orders/${order.id}`} aria-label={`View order ${order.id}`}>View order</Link>
        <button type="button" className="button-secondary" disabled={isReordering} onClick={onReorder}>
          {isReordering ? "Loading..." : "Reorder"}
        </button>
      </div>
    </article>
  );
}

function formatOrderDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

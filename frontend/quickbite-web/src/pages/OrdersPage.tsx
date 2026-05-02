import { FormEvent, useState } from "react";
import { ErrorState } from "../components/AsyncState";
import { ApiError } from "../lib/apiErrors";
import type { Order } from "../models";
import { getOrder } from "../services/quickbiteService";

export function OrdersPage() {
  const [order, setOrder] = useState<Order | null>(null);
  const [message, setMessage] = useState("Paste an order id after creating one via the Orders API.");
  const [error, setError] = useState<ApiError | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const orderId = String(formData.get("orderId") ?? "");

    try {
      setIsLoading(true);
      setError(null);
      const result = await getOrder(orderId);
      setOrder(result);
      setMessage("Order loaded successfully.");
    } catch (apiError) {
      setOrder(null);
      setError(apiError instanceof ApiError ? apiError : null);
      setMessage("Order lookup failed.");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="stack">
      <div>
        <p className="eyebrow">Orders</p>
        <h2>Order lookup</h2>
      </div>
      <form className="stack" onSubmit={handleSubmit}>
        <label>
          Order id
          <input name="orderId" placeholder="Order id" required />
        </label>
        <button type="submit" disabled={isLoading}>
          {isLoading ? "Loading..." : "Load order"}
        </button>
      </form>
      <p className="muted">{message}</p>
      {error && <ErrorState error={error} />}
      {order && (
        <article className="panel">
          <strong>{order.id}</strong>
          <p>Status: {order.status}</p>
          <p>Total: ${order.totalAmount.toFixed(2)}</p>
          <ul>
            {order.items.map((item) => (
              <li key={`${item.menuItemId}-${item.name}`}>
                {item.quantity} x {item.name} (${item.unitPrice.toFixed(2)})
              </li>
            ))}
          </ul>
        </article>
      )}
    </section>
  );
}

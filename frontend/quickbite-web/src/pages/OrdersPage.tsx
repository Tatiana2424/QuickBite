import { FormEvent, useState } from "react";
import type { Order } from "../models";
import { getOrder } from "../services/quickbiteService";

export function OrdersPage() {
  const [order, setOrder] = useState<Order | null>(null);
  const [message, setMessage] = useState("Paste an order id after creating one via the Orders API.");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const orderId = String(formData.get("orderId") ?? "");

    try {
      const result = await getOrder(orderId);
      setOrder(result);
      setMessage("Order loaded successfully.");
    } catch {
      setOrder(null);
      setMessage("Order was not found or the Orders API is unavailable.");
    }
  }

  return (
    <section className="stack">
      <div>
        <p className="eyebrow">Orders</p>
        <h2>Order lookup</h2>
      </div>
      <form className="stack" onSubmit={handleSubmit}>
        <input name="orderId" placeholder="Order id" />
        <button type="submit">Load order</button>
      </form>
      <p className="muted">{message}</p>
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

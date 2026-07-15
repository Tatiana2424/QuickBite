import { useMutation } from "@tanstack/react-query";
import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useCart } from "../cart/CartContext";
import { EmptyState, ErrorState } from "../components/AsyncState";
import type { DeliveryAddress } from "../models";
import { createOrder } from "../services/quickbiteService";

const defaultDeliveryAddress: DeliveryAddress = {
  line1: "123 Market Street",
  line2: "",
  city: "Seattle",
  state: "WA",
  postalCode: "98101",
  country: "USA"
};

export function CheckoutPage() {
  const navigate = useNavigate();
  const { items, itemCount, totalAmount, clearCart } = useCart();
  const [deliveryAddress, setDeliveryAddress] = useState<DeliveryAddress>(defaultDeliveryAddress);
  const checkoutMutation = useMutation({
    mutationFn: () =>
      createOrder({
        idempotencyKey: createIdempotencyKey(),
        deliveryAddress: normalizeAddress(deliveryAddress),
        items: items.map((item) => ({
          menuItemId: item.menuItemId,
          name: item.name,
          quantity: item.quantity,
          unitPrice: item.unitPrice
        }))
      }),
    onSuccess: (order) => {
      clearCart();
      navigate(`/orders/${order.id}`);
    }
  });

  function handleCheckout(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    checkoutMutation.mutate();
  }

  if (itemCount === 0) {
    return (
      <section className="stack">
        <div className="page-heading">
          <p className="eyebrow">Checkout</p>
          <h1>Checkout</h1>
          <p className="muted">Add items to your cart before placing an order.</p>
        </div>
        <EmptyState title="Your cart is empty">
          <Link className="button-link button-link--primary" to="/restaurants">Browse restaurants</Link>
        </EmptyState>
      </section>
    );
  }

  return (
    <section className="stack">
      <div className="page-heading">
        <p className="eyebrow">Checkout</p>
        <h1>Checkout</h1>
        <p className="muted">Confirm delivery details and place your order.</p>
      </div>
      <div className="checkout-layout">
        <form className="panel checkout-form" onSubmit={handleCheckout}>
          <div>
            <p className="eyebrow">Delivery</p>
            <h2>Where should we bring it?</h2>
          </div>
          <label>
            Street address
            <input
              name="line1"
              value={deliveryAddress.line1}
              onChange={(event) => setDeliveryAddress({ ...deliveryAddress, line1: event.target.value })}
              required
              maxLength={200}
            />
          </label>
          <label>
            Apt, suite, or notes
            <input
              name="line2"
              value={deliveryAddress.line2 ?? ""}
              onChange={(event) => setDeliveryAddress({ ...deliveryAddress, line2: event.target.value })}
              maxLength={200}
            />
          </label>
          <div className="checkout-address-grid">
            <label>
              City
              <input
                name="city"
                value={deliveryAddress.city}
                onChange={(event) => setDeliveryAddress({ ...deliveryAddress, city: event.target.value })}
                required
                maxLength={120}
              />
            </label>
            <label>
              State
              <input
                name="state"
                value={deliveryAddress.state}
                onChange={(event) => setDeliveryAddress({ ...deliveryAddress, state: event.target.value })}
                required
                maxLength={80}
              />
            </label>
          </div>
          <div className="checkout-address-grid">
            <label>
              ZIP
              <input
                name="postalCode"
                value={deliveryAddress.postalCode}
                onChange={(event) => setDeliveryAddress({ ...deliveryAddress, postalCode: event.target.value })}
                required
                maxLength={20}
              />
            </label>
            <label>
              Country
              <input
                name="country"
                value={deliveryAddress.country}
                onChange={(event) => setDeliveryAddress({ ...deliveryAddress, country: event.target.value })}
                required
                maxLength={80}
              />
            </label>
          </div>
          <button type="submit" disabled={checkoutMutation.isPending}>
            {checkoutMutation.isPending ? "Placing order..." : "Place order"}
          </button>
          {checkoutMutation.isError && <ErrorState error={checkoutMutation.error} />}
        </form>
        <aside className="panel checkout-summary" aria-label="Checkout order summary">
          <div>
            <p className="eyebrow">Order</p>
            <h2>Review your order</h2>
          </div>
          <div className="cart-lines">
            {items.map((item) => (
              <article key={item.menuItemId} className="checkout-line">
                <div>
                  <strong>{item.name}</strong>
                  <small>{item.quantity} x ${item.unitPrice.toFixed(2)}</small>
                </div>
                <strong>${(item.quantity * item.unitPrice).toFixed(2)}</strong>
              </article>
            ))}
          </div>
          <div className="cart-total cart-total--large">
            <span>{itemCount} item{itemCount === 1 ? "" : "s"}</span>
            <strong>${totalAmount.toFixed(2)}</strong>
          </div>
          <Link className="text-link" to="/cart">Edit cart</Link>
        </aside>
      </div>
    </section>
  );
}

function normalizeAddress(address: DeliveryAddress): DeliveryAddress {
  return {
    line1: address.line1.trim(),
    line2: address.line2?.trim() || null,
    city: address.city.trim(),
    state: address.state.trim(),
    postalCode: address.postalCode.trim(),
    country: address.country.trim()
  };
}

function createIdempotencyKey() {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `checkout-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

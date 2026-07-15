import { Link, useLocation, useNavigate } from "react-router-dom";
import { useCart } from "../cart/CartContext";
import { EmptyState } from "../components/AsyncState";

export function CartPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { items, itemCount, totalAmount, setQuantity, removeItem, clearCart } = useCart();
  const isReorderCart = items.some((item) => item.restaurantId.startsWith("reorder-"));
  const reorderSource = typeof location.state === "object" && location.state !== null && "reorderSource" in location.state
    ? String(location.state.reorderSource)
    : null;

  if (itemCount === 0) {
    return (
      <section className="stack">
        <div className="page-heading">
          <p className="eyebrow">Cart</p>
          <h1>Your cart</h1>
          <p className="muted">Choose a restaurant and add a few favorites when you are ready.</p>
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
        <p className="eyebrow">Cart</p>
        <h1>Your cart</h1>
        <p className="muted">Review quantities, then continue to checkout for delivery details.</p>
      </div>
      <section className="panel cart-page-panel" aria-label="Cart items">
        {isReorderCart && (
          <div className="notice-card" role="status">
            <strong>Reorder started{reorderSource ? ` from order ${reorderSource.slice(0, 8)}` : ""}.</strong>
            <p className="muted">Review quantities before checkout. If a menu item changed, checkout will show the latest order result from the API.</p>
          </div>
        )}
        <div className="cart-lines">
          {items.map((item) => (
            <article key={item.menuItemId} className="cart-line">
              <div>
                <strong>{item.name}</strong>
                <small>${item.unitPrice.toFixed(2)} each</small>
              </div>
              <div className="quantity-control" aria-label={`${item.name} quantity`}>
                <button type="button" className="button-icon" aria-label={`Decrease ${item.name}`} onClick={() => setQuantity(item.menuItemId, item.quantity - 1)}>
                  -
                </button>
                <span>{item.quantity}</span>
                <button type="button" className="button-icon" aria-label={`Increase ${item.name}`} onClick={() => setQuantity(item.menuItemId, item.quantity + 1)}>
                  +
                </button>
              </div>
              <strong>${(item.quantity * item.unitPrice).toFixed(2)}</strong>
              <button type="button" className="button-secondary" onClick={() => removeItem(item.menuItemId)}>
                Remove
              </button>
            </article>
          ))}
        </div>
        <div className="cart-total cart-total--large">
          <span>{itemCount} item{itemCount === 1 ? "" : "s"}</span>
          <strong>${totalAmount.toFixed(2)}</strong>
        </div>
        <div className="cart-page-actions">
          <button type="button" className="button-secondary" onClick={clearCart}>
            Clear cart
          </button>
          <button type="button" onClick={() => navigate("/checkout")}>
            Continue checkout
          </button>
        </div>
      </section>
    </section>
  );
}

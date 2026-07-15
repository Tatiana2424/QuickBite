import { useQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { useCart } from "../cart/CartContext";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import { getRestaurantDetails } from "../services/quickbiteService";

export function RestaurantDetailsPage() {
  const { restaurantId = "" } = useParams();
  const navigate = useNavigate();
  const { items, itemCount, totalAmount, addItem, setQuantity, removeItem } = useCart();
  const restaurantQuery = useQuery({
    queryKey: ["restaurant-details", restaurantId],
    queryFn: () => getRestaurantDetails(restaurantId),
    enabled: Boolean(restaurantId)
  });

  if (restaurantQuery.isLoading) {
    return <LoadingState label="Loading restaurant details..." />;
  }

  if (restaurantQuery.isError || !restaurantQuery.data) {
    return <ErrorState error={restaurantQuery.error} action={<button type="button" onClick={() => void restaurantQuery.refetch()}>Retry</button>} />;
  }

  return (
    <section className="stack">
      <div>
        <p className="eyebrow">{restaurantQuery.data.cuisine}</p>
        <h2>{restaurantQuery.data.name}</h2>
        <p>{restaurantQuery.data.description}</p>
      </div>
      {restaurantQuery.data.menuItems.length === 0 && (
        <EmptyState title="No menu items">This restaurant does not have menu data yet.</EmptyState>
      )}
      <div className="grid">
        {restaurantQuery.data.menuItems.map((item) => (
          <article key={item.id} className="card menu-item-card">
            <div>
              <strong>{item.name}</strong>
              <p>{item.description}</p>
            </div>
            <div className="menu-item-card__footer">
              <span>${item.price.toFixed(2)}</span>
              <button type="button" onClick={() => addItem(item)}>
                Add to cart
              </button>
            </div>
          </article>
        ))}
      </div>
      <aside className="panel cart-summary" aria-label="Cart summary">
        <div>
          <p className="eyebrow">Cart</p>
          <h3>Your bag</h3>
        </div>
        {itemCount === 0 ? (
          <p className="muted">Add something tasty to start your order.</p>
        ) : (
          <>
            <div className="cart-lines">
              {items.map((item) => (
                <CartLineRow
                  key={item.menuItemId}
                  item={item}
                  onDecrease={() => setQuantity(item.menuItemId, item.quantity - 1)}
                  onIncrease={() => setQuantity(item.menuItemId, item.quantity + 1)}
                  onRemove={() => removeItem(item.menuItemId)}
                />
              ))}
            </div>
            <div className="cart-total">
              <span>Total</span>
              <strong>${totalAmount.toFixed(2)}</strong>
            </div>
            <button type="button" onClick={() => navigate("/cart")}>
              Review cart
            </button>
          </>
        )}
      </aside>
    </section>
  );
}
function CartLineRow({
  item,
  onDecrease,
  onIncrease,
  onRemove
}: {
  item: { name: string; quantity: number; unitPrice: number };
  onDecrease: () => void;
  onIncrease: () => void;
  onRemove: () => void;
}) {
  return (
    <article className="cart-line">
      <div>
        <strong>{item.name}</strong>
        <small>${item.unitPrice.toFixed(2)} each</small>
      </div>
      <div className="quantity-control" aria-label={`${item.name} quantity`}>
        <button type="button" className="button-icon" aria-label={`Decrease ${item.name}`} onClick={onDecrease}>
          -
        </button>
        <span>{item.quantity}</span>
        <button type="button" className="button-icon" aria-label={`Increase ${item.name}`} onClick={onIncrease}>
          +
        </button>
      </div>
      <strong>${(item.quantity * item.unitPrice).toFixed(2)}</strong>
      <button type="button" className="button-secondary" onClick={onRemove}>
        Remove
      </button>
    </article>
  );
}

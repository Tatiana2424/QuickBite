import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { useCart } from "../cart/CartContext";
import { ErrorState, LoadingState } from "../components/AsyncState";
import { useFavoriteRestaurants } from "../favorites/useFavoriteRestaurants";
import type { RestaurantSummary } from "../models";
import { getMyOrders, getRestaurants } from "../services/quickbiteService";

export function HomePage() {
  const { isAuthenticated, user } = useAuth();
  const { itemCount, totalAmount } = useCart();
  const { favoriteRestaurantIds, isFavorite, toggleFavorite } = useFavoriteRestaurants();
  const restaurantsQuery = useQuery({
    queryKey: ["restaurants"],
    queryFn: getRestaurants
  });
  const ordersQuery = useQuery({
    queryKey: ["my-orders", "home"],
    queryFn: () => getMyOrders(3),
    enabled: isAuthenticated
  });

  const restaurants = restaurantsQuery.data ?? [];
  const featuredRestaurants = restaurants.slice(0, 4);
  const favoriteRestaurants = favoriteRestaurantIds
    .map((restaurantId) => restaurants.find((restaurant) => restaurant.id === restaurantId))
    .filter((restaurant): restaurant is RestaurantSummary => Boolean(restaurant));
  const cuisines = Array.from(new Set(restaurants.map((restaurant) => restaurant.cuisine))).slice(0, 8);
  const recentOrder = ordersQuery.data?.[0];

  return (
    <section className="stack">
      <section className="customer-hero" aria-labelledby="home-title">
        <div className="page-heading">
          <p className="eyebrow">{isAuthenticated && user ? `Welcome back, ${user.fullName}` : "Dinner starts here"}</p>
          <h1 id="home-title">Order something good without the guesswork.</h1>
          <p className="muted">Jump into local favorites, check your cart, or keep an eye on orders already moving through payment and delivery.</p>
          <div className="hero-actions">
            <Link className="button-link button-link--primary" to="/restaurants">Browse restaurants</Link>
            {isAuthenticated ? (
              <Link className="button-link" to="/orders">Track orders</Link>
            ) : (
              <Link className="button-link" to="/register">Create account</Link>
            )}
          </div>
        </div>
        <aside className="hero-status-panel" aria-label="Quick status">
          <div>
            <span className="status-label">Cart</span>
            <strong>{itemCount} item{itemCount === 1 ? "" : "s"}</strong>
            <p className="muted">${totalAmount.toFixed(2)} ready for checkout</p>
          </div>
          <Link className="text-link" to={itemCount > 0 ? "/cart" : "/restaurants"}>
            {itemCount > 0 ? "Review cart" : "Start an order"}
          </Link>
        </aside>
      </section>

      <section className="home-action-grid" aria-label="Quick actions">
        <QuickAction title="Browse by craving" copy="Search cuisines, restaurants, and quick meals." to="/restaurants" />
        <QuickAction title="Review your cart" copy={itemCount > 0 ? `${itemCount} items waiting.` : "Add items from any restaurant."} to="/cart" />
        <QuickAction title="Track delivery" copy={isAuthenticated ? "Follow payment and courier progress." : "Sign in to see order progress."} to={isAuthenticated ? "/orders" : "/login"} />
      </section>

      {restaurantsQuery.isLoading && <LoadingState label="Loading local picks..." />}
      {restaurantsQuery.isError && <ErrorState error={restaurantsQuery.error} action={<button type="button" onClick={() => void restaurantsQuery.refetch()}>Retry</button>} />}

      {restaurants.length > 0 && (
        <>
          <section className="featured-section" aria-labelledby="home-featured-title">
            <div className="section-heading section-heading--row">
              <div>
                <p className="eyebrow">Featured nearby</p>
                <h2 id="home-featured-title">Good places to start</h2>
              </div>
              <Link className="text-link" to="/restaurants">See all restaurants</Link>
            </div>
            <div className="featured-grid">
              {featuredRestaurants.map((restaurant, index) => (
                <HomeRestaurantCard
                  key={restaurant.id}
                  restaurant={restaurant}
                  featuredRank={index + 1}
                  isFavorite={isFavorite(restaurant.id)}
                  onToggleFavorite={() => toggleFavorite(restaurant.id)}
                />
              ))}
            </div>
          </section>

          {favoriteRestaurants.length > 0 && (
            <section className="panel favorites-panel" aria-label="Favorite restaurants">
              <div className="section-heading section-heading--row">
                <div>
                  <p className="eyebrow">Favorites</p>
                  <h2>Restaurants you saved</h2>
                </div>
                <Link className="text-link" to="/restaurants">Manage favorites</Link>
              </div>
              <div className="favorite-list">
                {favoriteRestaurants.slice(0, 4).map((restaurant) => (
                  <Link key={restaurant.id} className="favorite-pill" to={`/restaurants/${restaurant.id}`}>
                    <span>{restaurant.name}</span>
                    <small>{restaurant.cuisine}</small>
                  </Link>
                ))}
              </div>
            </section>
          )}

          <section className="panel cuisine-shortcuts" aria-label="Cuisine shortcuts">
            <div className="section-heading">
              <p className="eyebrow">Explore cuisines</p>
              <h2>Pick a direction</h2>
            </div>
            <div className="filter-chip-group">
              {cuisines.map((cuisine) => (
                <Link key={cuisine} className="filter-chip cuisine-link" to={`/restaurants?cuisine=${encodeURIComponent(cuisine)}`}>
                  {cuisine}
                </Link>
              ))}
            </div>
          </section>
        </>
      )}

      {isAuthenticated ? (
        <section className="panel home-order-panel" aria-label="Recent order shortcut">
          <div>
            <p className="eyebrow">Your orders</p>
            <h2>{recentOrder ? "Latest order" : "No orders yet"}</h2>
            <p className="muted">
              {recentOrder
                ? `${recentOrder.itemSummary} is currently ${recentOrder.status}.`
                : "Your first checkout will show up here with payment and delivery progress."}
            </p>
          </div>
          <Link className="button-link button-link--primary" to={recentOrder ? `/orders/${recentOrder.id}` : "/restaurants"}>
            {recentOrder ? "View order" : "Find food"}
          </Link>
        </section>
      ) : (
        <section className="panel home-order-panel" aria-label="Account prompt">
          <div>
            <p className="eyebrow">Save your progress</p>
            <h2>Create an account to track every step.</h2>
            <p className="muted">QuickBite keeps your orders, payment status, and delivery updates in one place.</p>
          </div>
          <Link className="button-link button-link--primary" to="/register">Create account</Link>
        </section>
      )}
    </section>
  );
}

function QuickAction({ title, copy, to }: { title: string; copy: string; to: string }) {
  return (
    <Link className="card quick-action-card" to={to}>
      <strong>{title}</strong>
      <p className="muted">{copy}</p>
    </Link>
  );
}

function HomeRestaurantCard({
  restaurant,
  featuredRank,
  isFavorite,
  onToggleFavorite
}: {
  restaurant: RestaurantSummary;
  featuredRank: number;
  isFavorite: boolean;
  onToggleFavorite: () => void;
}) {
  return (
    <article className="card restaurant-card">
      <RestaurantVisual restaurant={restaurant} />
      <div className="restaurant-card__meta">
        <span className="cuisine-chip">{restaurant.cuisine}</span>
        <span className="rank-chip">#{featuredRank}</span>
      </div>
      <Link to={`/restaurants/${restaurant.id}`} className="restaurant-card__link">
        <strong>{restaurant.name}</strong>
      </Link>
      <p>{restaurant.description}</p>
      <div className="restaurant-card__stats" aria-label={`${restaurant.name} delivery details`}>
        <span>{formatRating(restaurant.rating)}</span>
        <span>{restaurant.estimatedDeliveryMinutes ?? 30} min</span>
        <span>{formatFee(restaurant.deliveryFee)}</span>
      </div>
      <button
        type="button"
        className="button-secondary favorite-button"
        aria-pressed={isFavorite}
        onClick={onToggleFavorite}
      >
        {isFavorite ? "Saved favorite" : "Save favorite"}
      </button>
    </article>
  );
}

function RestaurantVisual({ restaurant }: { restaurant: RestaurantSummary }) {
  if (!restaurant.imageUrl) {
    return <div className="restaurant-card__fallback" aria-hidden="true">{restaurant.name.slice(0, 2).toUpperCase()}</div>;
  }

  return <img className="restaurant-card__image" src={restaurant.imageUrl} alt="" loading="lazy" />;
}

function formatRating(value?: number) {
  return `${(value ?? 4.6).toFixed(1)} stars`;
}

function formatFee(value?: number) {
  return !value || value === 0 ? "Free delivery" : `$${value.toFixed(2)} delivery`;
}

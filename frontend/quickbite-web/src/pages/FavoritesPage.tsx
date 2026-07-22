import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import { useFavoriteRestaurants } from "../favorites/useFavoriteRestaurants";
import type { RestaurantSummary } from "../models";
import { getRestaurants } from "../services/quickbiteService";

export function FavoritesPage() {
  const { favoriteRestaurantIds, toggleFavorite } = useFavoriteRestaurants();
  const restaurantsQuery = useQuery({
    queryKey: ["restaurants"],
    queryFn: getRestaurants
  });
  const restaurants = restaurantsQuery.data ?? [];
  const favoriteRestaurants = favoriteRestaurantIds
    .map((restaurantId) => restaurants.find((restaurant) => restaurant.id === restaurantId))
    .filter((restaurant): restaurant is RestaurantSummary => Boolean(restaurant));

  return (
    <section className="stack">
      <section className="catalog-hero">
        <div className="page-heading">
          <p className="eyebrow">Favorites</p>
          <h1>Your saved restaurants</h1>
          <p className="muted">Keep your regular spots in one place and jump back into ordering faster.</p>
        </div>
        <Link className="button-link button-link--primary" to="/restaurants">Manage favorites</Link>
      </section>

      {restaurantsQuery.isLoading && <LoadingState label="Loading favorites..." />}
      {restaurantsQuery.isError && <ErrorState error={restaurantsQuery.error} action={<button type="button" onClick={() => void restaurantsQuery.refetch()}>Retry</button>} />}
      {restaurantsQuery.isSuccess && favoriteRestaurants.length === 0 && (
        <EmptyState title="No saved restaurants yet">
          Browse restaurants and use Save favorite to build your shortcut list.
        </EmptyState>
      )}
      {favoriteRestaurants.length > 0 && (
        <section className="favorites-page" aria-labelledby="favorite-restaurants-title">
          <div className="section-heading section-heading--row">
            <div>
              <p className="eyebrow">Saved</p>
              <h2 id="favorite-restaurants-title">Favorite restaurants</h2>
            </div>
            <span className="muted">{favoriteRestaurants.length} saved</span>
          </div>
          <div className="grid">
            {favoriteRestaurants.map((restaurant) => (
              <FavoriteRestaurantCard
                key={restaurant.id}
                restaurant={restaurant}
                onRemove={() => toggleFavorite(restaurant.id)}
              />
            ))}
          </div>
        </section>
      )}
    </section>
  );
}

function FavoriteRestaurantCard({ restaurant, onRemove }: { restaurant: RestaurantSummary; onRemove: () => void }) {
  return (
    <article className="card restaurant-card">
      <RestaurantVisual restaurant={restaurant} />
      <div className="restaurant-card__meta">
        <span className="cuisine-chip">{restaurant.cuisine}</span>
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
      <button type="button" className="button-secondary favorite-button" onClick={onRemove}>
        Remove favorite
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

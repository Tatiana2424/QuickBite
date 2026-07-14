import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import { getRestaurants } from "../services/quickbiteService";

export function RestaurantsPage() {
  const restaurantsQuery = useQuery({
    queryKey: ["restaurants"],
    queryFn: getRestaurants
  });

  return (
    <section className="stack">
      <div className="page-heading">
        <p className="eyebrow">Restaurants near you</p>
        <h2>Choose dinner in a few taps</h2>
        <p className="muted">Browse local favorites, add what looks good, and track every order from checkout to delivery.</p>
      </div>
      {restaurantsQuery.isLoading && <LoadingState label="Loading restaurants..." />}
      {restaurantsQuery.isError && <ErrorState error={restaurantsQuery.error} action={<button type="button" onClick={() => void restaurantsQuery.refetch()}>Retry</button>} />}
      {restaurantsQuery.isSuccess && restaurantsQuery.data.length === 0 && (
        <EmptyState title="No restaurants yet">Catalog seed data has not been loaded for this environment.</EmptyState>
      )}
      <div className="grid">
        {restaurantsQuery.data?.map((restaurant) => (
          <Link key={restaurant.id} to={`/restaurants/${restaurant.id}`} className="card restaurant-card">
            <span className="cuisine-chip">{restaurant.cuisine}</span>
            <strong>{restaurant.name}</strong>
            <p>{restaurant.description}</p>
          </Link>
        ))}
      </div>
    </section>
  );
}

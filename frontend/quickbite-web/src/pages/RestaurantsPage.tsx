import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getRestaurants } from "../services/quickbiteService";

export function RestaurantsPage() {
  const restaurantsQuery = useQuery({
    queryKey: ["restaurants"],
    queryFn: getRestaurants
  });

  return (
    <section className="stack">
      <div>
        <p className="eyebrow">Catalog</p>
        <h2>Restaurants</h2>
      </div>
      {restaurantsQuery.isLoading && <p className="muted">Loading restaurants...</p>}
      {restaurantsQuery.isError && <p className="muted">The catalog gateway is not reachable yet.</p>}
      <div className="grid">
        {restaurantsQuery.data?.map((restaurant) => (
          <Link key={restaurant.id} to={`/restaurants/${restaurant.id}`} className="card">
            <strong>{restaurant.name}</strong>
            <span>{restaurant.cuisine}</span>
            <p>{restaurant.description}</p>
          </Link>
        ))}
      </div>
    </section>
  );
}

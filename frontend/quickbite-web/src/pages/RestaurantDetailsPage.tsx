import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import { getRestaurantDetails } from "../services/quickbiteService";

export function RestaurantDetailsPage() {
  const { restaurantId = "" } = useParams();
  const restaurantQuery = useQuery({
    queryKey: ["restaurant-details", restaurantId],
    queryFn: () => getRestaurantDetails(restaurantId),
    enabled: Boolean(restaurantId)
  });

  if (restaurantQuery.isLoading) {
    return <p className="muted">Loading restaurant details...</p>;
  }

  if (restaurantQuery.isError || !restaurantQuery.data) {
    return <p className="muted">Unable to load the restaurant details through the gateway.</p>;
  }

  return (
    <section className="stack">
      <div>
        <p className="eyebrow">{restaurantQuery.data.cuisine}</p>
        <h2>{restaurantQuery.data.name}</h2>
        <p>{restaurantQuery.data.description}</p>
      </div>
      <div className="grid">
        {restaurantQuery.data.menuItems.map((item) => (
          <article key={item.id} className="card">
            <strong>{item.name}</strong>
            <p>{item.description}</p>
            <span>${item.price.toFixed(2)}</span>
          </article>
        ))}
      </div>
    </section>
  );
}

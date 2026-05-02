import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import { getRestaurantDetails } from "../services/quickbiteService";

export function RestaurantDetailsPage() {
  const { restaurantId = "" } = useParams();
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

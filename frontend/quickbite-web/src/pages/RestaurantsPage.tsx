import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import { RestaurantSummary } from "../models";
import { getRestaurants } from "../services/quickbiteService";

export function RestaurantsPage() {
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedCuisine, setSelectedCuisine] = useState("All");
  const restaurantsQuery = useQuery({
    queryKey: ["restaurants"],
    queryFn: getRestaurants
  });
  const restaurants = restaurantsQuery.data ?? [];
  const cuisines = useMemo(() => {
    return ["All", ...Array.from(new Set(restaurants.map((restaurant) => restaurant.cuisine))).sort()];
  }, [restaurants]);
  const featuredRestaurants = restaurants.slice(0, 3);
  const filteredRestaurants = useMemo(() => {
    const normalizedSearch = searchTerm.trim().toLocaleLowerCase();

    return restaurants.filter((restaurant) => {
      const matchesCuisine = selectedCuisine === "All" || restaurant.cuisine === selectedCuisine;
      const matchesSearch = normalizedSearch.length === 0
        || restaurant.name.toLocaleLowerCase().includes(normalizedSearch)
        || restaurant.cuisine.toLocaleLowerCase().includes(normalizedSearch);

      return matchesCuisine && matchesSearch;
    });
  }, [restaurants, searchTerm, selectedCuisine]);
  const hasFilters = searchTerm.trim().length > 0 || selectedCuisine !== "All";

  return (
    <section className="stack">
      <div className="home-hero">
        <div className="page-heading">
          <p className="eyebrow">Restaurants near you</p>
          <h1>Find the right meal, then checkout fast.</h1>
          <p className="muted">Search local favorites by name or cuisine, compare what looks good, and track every order from payment to delivery.</p>
        </div>
        <a className="button-link button-link--primary" href="#restaurant-discovery">Browse restaurants</a>
      </div>
      {restaurantsQuery.isLoading && <LoadingState label="Loading restaurants..." />}
      {restaurantsQuery.isError && <ErrorState error={restaurantsQuery.error} action={<button type="button" onClick={() => void restaurantsQuery.refetch()}>Retry</button>} />}
      {restaurantsQuery.isSuccess && restaurantsQuery.data.length === 0 && (
        <EmptyState title="No restaurants yet">Catalog seed data has not been loaded for this environment.</EmptyState>
      )}
      {restaurantsQuery.isSuccess && restaurants.length > 0 && (
        <>
          <section className="featured-section" aria-labelledby="featured-restaurants">
            <div className="section-heading">
              <p className="eyebrow">Featured</p>
              <h2 id="featured-restaurants">Popular picks today</h2>
            </div>
            <div className="featured-grid">
              {featuredRestaurants.map((restaurant, index) => (
                <RestaurantCard key={restaurant.id} restaurant={restaurant} featuredRank={index + 1} />
              ))}
            </div>
          </section>
          <section id="restaurant-discovery" className="discovery-panel" aria-labelledby="restaurant-discovery-title">
            <div className="section-heading">
              <p className="eyebrow">Discover</p>
              <h2 id="restaurant-discovery-title">All restaurants</h2>
            </div>
            <div className="discovery-controls">
              <label className="search-field">
                Search restaurants
                <input
                  type="search"
                  value={searchTerm}
                  onChange={(event) => setSearchTerm(event.target.value)}
                  placeholder="Try pizza, healthy, Urban Bowl..."
                />
              </label>
              <div className="filter-chip-group" aria-label="Filter restaurants by cuisine">
                {cuisines.map((cuisine) => (
                  <button
                    key={cuisine}
                    type="button"
                    className={selectedCuisine === cuisine ? "filter-chip filter-chip--active" : "filter-chip"}
                    aria-pressed={selectedCuisine === cuisine}
                    onClick={() => setSelectedCuisine(cuisine)}
                  >
                    {cuisine}
                  </button>
                ))}
              </div>
            </div>
            <p className="muted">
              Showing {filteredRestaurants.length} of {restaurants.length} restaurants
            </p>
            {filteredRestaurants.length === 0 ? (
              <EmptyState title="No matches">Try a different restaurant name or cuisine.</EmptyState>
            ) : (
              <div className="grid">
                {filteredRestaurants.map((restaurant) => (
                  <RestaurantCard key={restaurant.id} restaurant={restaurant} />
                ))}
              </div>
            )}
            {hasFilters && (
              <button
                type="button"
                className="button-secondary reset-filters"
                onClick={() => {
                  setSearchTerm("");
                  setSelectedCuisine("All");
                }}
              >
                Clear search and filters
              </button>
            )}
          </section>
        </>
      )}
    </section>
  );
}

function RestaurantCard({
  restaurant,
  featuredRank
}: {
  restaurant: RestaurantSummary;
  featuredRank?: number;
}) {
  return (
    <Link to={`/restaurants/${restaurant.id}`} className="card restaurant-card">
      <div className="restaurant-card__meta">
        <span className="cuisine-chip">{restaurant.cuisine}</span>
        {featuredRank && <span className="rank-chip">#{featuredRank}</span>}
      </div>
      <strong>{restaurant.name}</strong>
      <p>{restaurant.description}</p>
    </Link>
  );
}

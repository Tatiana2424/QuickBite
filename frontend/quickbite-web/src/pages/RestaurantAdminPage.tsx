import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FormEvent, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { EmptyState, ErrorState, LoadingState } from "../components/AsyncState";
import type { MenuItem, RestaurantSummary } from "../models";
import {
  createMenuItem,
  createRestaurant,
  getRestaurantDetails,
  getRestaurants,
  updateMenuItem,
  updateRestaurant
} from "../services/quickbiteService";

export function RestaurantAdminPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canManage = user?.roles.some((role) => role === "RestaurantAdmin" || role === "PlatformAdmin");
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<string | null>(null);
  const [restaurantForm, setRestaurantForm] = useState({ name: "", cuisine: "", description: "" });
  const [menuForm, setMenuForm] = useState({ id: "", name: "", description: "", price: "9.99" });
  const restaurantsQuery = useQuery({ queryKey: ["restaurants"], queryFn: getRestaurants, enabled: canManage });
  const selectedRestaurant = selectedRestaurantId ?? restaurantsQuery.data?.[0]?.id ?? "";
  const detailsQuery = useQuery({
    queryKey: ["restaurant-details", selectedRestaurant],
    queryFn: () => getRestaurantDetails(selectedRestaurant),
    enabled: canManage && Boolean(selectedRestaurant)
  });
  const restaurantMutation = useMutation({
    mutationFn: () => restaurantForm.name && selectedRestaurantId
      ? updateRestaurant(selectedRestaurantId, restaurantForm)
      : createRestaurant(restaurantForm),
    onSuccess: async (restaurant) => {
      setSelectedRestaurantId(restaurant.id);
      setRestaurantForm({ name: "", cuisine: "", description: "" });
      await queryClient.invalidateQueries({ queryKey: ["restaurants"] });
      await queryClient.invalidateQueries({ queryKey: ["restaurant-details", restaurant.id] });
    }
  });
  const menuMutation = useMutation({
    mutationFn: () => {
      const payload = { name: menuForm.name, description: menuForm.description, price: Number(menuForm.price) };
      return menuForm.id
        ? updateMenuItem(selectedRestaurant, menuForm.id, payload)
        : createMenuItem(selectedRestaurant, payload);
    },
    onSuccess: async () => {
      setMenuForm({ id: "", name: "", description: "", price: "9.99" });
      await queryClient.invalidateQueries({ queryKey: ["restaurant-details", selectedRestaurant] });
    }
  });

  if (!canManage) {
    return <ErrorState error={new Error("You need a RestaurantAdmin role to manage restaurants.")} />;
  }

  return (
    <section className="stack">
      <div className="page-heading">
        <p className="eyebrow">Restaurant admin</p>
        <h2>Manage restaurants and menus</h2>
        <p className="muted">Create restaurants, update customer-facing copy, and keep menu items ready for ordering.</p>
      </div>
      {restaurantsQuery.isLoading && <LoadingState label="Loading restaurants..." />}
      {restaurantsQuery.isError && <ErrorState error={restaurantsQuery.error} action={<button type="button" onClick={() => void restaurantsQuery.refetch()}>Retry</button>} />}
      {restaurantsQuery.isSuccess && restaurantsQuery.data.length === 0 && (
        <EmptyState title="No restaurants yet">Create the first restaurant below.</EmptyState>
      )}
      <div className="admin-layout">
        <section className="panel admin-panel" aria-label="Restaurant list">
          <div className="section-heading">
            <p className="eyebrow">Restaurants</p>
            <h3>Catalog</h3>
          </div>
          <div className="admin-list">
            {restaurantsQuery.data?.map((restaurant) => (
              <button
                key={restaurant.id}
                type="button"
                className={selectedRestaurant === restaurant.id ? "admin-list-item admin-list-item--active" : "admin-list-item"}
                onClick={() => {
                  setSelectedRestaurantId(restaurant.id);
                  setRestaurantForm(fromRestaurant(restaurant));
                }}
              >
                <strong>{restaurant.name}</strong>
                <span>{restaurant.cuisine}</span>
              </button>
            ))}
          </div>
        </section>
        <section className="panel admin-panel" aria-label="Restaurant editor">
          <form className="stack" onSubmit={(event) => {
            event.preventDefault();
            restaurantMutation.mutate();
          }}>
          <div className="section-heading">
            <p className="eyebrow">Restaurant</p>
            <h3>{selectedRestaurantId ? "Edit restaurant" : "Create restaurant"}</h3>
          </div>
            {selectedRestaurantId && (
              <button
                type="button"
                className="button-secondary"
                onClick={() => {
                  setSelectedRestaurantId(null);
                  setRestaurantForm({ name: "", cuisine: "", description: "" });
                }}
              >
                New restaurant
              </button>
            )}
            <label>Name<input value={restaurantForm.name} onChange={(event) => setRestaurantForm({ ...restaurantForm, name: event.target.value })} required maxLength={200} /></label>
            <label>Cuisine<input value={restaurantForm.cuisine} onChange={(event) => setRestaurantForm({ ...restaurantForm, cuisine: event.target.value })} required maxLength={100} /></label>
            <label>Description<input value={restaurantForm.description} onChange={(event) => setRestaurantForm({ ...restaurantForm, description: event.target.value })} required maxLength={500} /></label>
            <button type="submit" disabled={restaurantMutation.isPending}>{restaurantMutation.isPending ? "Saving..." : "Save restaurant"}</button>
          </form>
        </section>
      </div>
      {detailsQuery.isLoading && <LoadingState label="Loading menu..." />}
      {detailsQuery.data && (
        <section className="panel admin-panel" aria-label="Menu manager">
          <div className="section-heading">
            <p className="eyebrow">Menu</p>
            <h3>{detailsQuery.data.name}</h3>
          </div>
          <div className="admin-menu-grid">
            {detailsQuery.data.menuItems.map((item) => (
              <article key={item.id} className="card menu-item-card">
                <strong>{item.name}</strong>
                <p>{item.description}</p>
                <div className="menu-item-card__footer">
                  <span>${item.price.toFixed(2)}</span>
                  <button type="button" className="button-secondary" onClick={() => setMenuForm(fromMenuItem(item))}>Edit</button>
                </div>
              </article>
            ))}
          </div>
          <form className="stack" onSubmit={(event) => submitMenu(event, menuMutation.mutate)}>
            <h3>{menuForm.id ? "Edit menu item" : "Create menu item"}</h3>
            <label>Name<input value={menuForm.name} onChange={(event) => setMenuForm({ ...menuForm, name: event.target.value })} required maxLength={200} /></label>
            <label>Description<input value={menuForm.description} onChange={(event) => setMenuForm({ ...menuForm, description: event.target.value })} required maxLength={500} /></label>
            <label>Price<input type="number" min="0.01" step="0.01" value={menuForm.price} onChange={(event) => setMenuForm({ ...menuForm, price: event.target.value })} required /></label>
            <button type="submit" disabled={menuMutation.isPending || !selectedRestaurant}>{menuMutation.isPending ? "Saving..." : "Save menu item"}</button>
          </form>
        </section>
      )}
    </section>
  );
}

function fromRestaurant(restaurant: RestaurantSummary) {
  return { name: restaurant.name, cuisine: restaurant.cuisine, description: restaurant.description };
}

function fromMenuItem(item: MenuItem) {
  return { id: item.id, name: item.name, description: item.description, price: item.price.toFixed(2) };
}

function submitMenu(event: FormEvent<HTMLFormElement>, mutate: () => void) {
  event.preventDefault();
  mutate();
}

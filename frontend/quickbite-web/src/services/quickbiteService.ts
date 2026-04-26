import { apiClient } from "../lib/api";
import type { Order, RestaurantDetails, RestaurantSummary } from "../models";

export async function getRestaurants(): Promise<RestaurantSummary[]> {
  const response = await apiClient.get<RestaurantSummary[]>("/catalog/api/restaurants");
  return response.data;
}

export async function getRestaurantDetails(restaurantId: string): Promise<RestaurantDetails> {
  const response = await apiClient.get<RestaurantDetails>(`/catalog/api/restaurants/${restaurantId}`);
  return response.data;
}

export async function getOrder(orderId: string): Promise<Order> {
  const response = await apiClient.get<Order>(`/orders/api/orders/${orderId}`);
  return response.data;
}

export async function login(email: string, password: string) {
  const response = await apiClient.post("/identity/api/auth/login", { email, password });
  return response.data as { accessToken: string; refreshToken: string; fullName: string };
}

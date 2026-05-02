import { anonymousApiClient, apiClient } from "../lib/api";
import type { AuthResponse, Order, RestaurantDetails, RestaurantSummary } from "../models";

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

export async function login(email: string, password: string): Promise<AuthResponse> {
  const response = await anonymousApiClient.post<AuthResponse>("/identity/api/auth/login", { email, password });
  return response.data;
}

export async function refreshSession(refreshToken: string): Promise<AuthResponse> {
  const response = await anonymousApiClient.post<AuthResponse>("/identity/api/auth/refresh", { refreshToken });
  return response.data;
}

export async function logout(refreshToken: string): Promise<void> {
  await anonymousApiClient.post("/identity/api/auth/logout", { refreshToken });
}

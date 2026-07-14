import { anonymousApiClient, apiClient } from "../lib/api";
import { ApiError } from "../lib/apiErrors";
import type { AuthResponse, CreateOrderRequest, Delivery, Order, Payment, RestaurantDetails, RestaurantSummary } from "../models";

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

export async function getPaymentForOrder(orderId: string): Promise<Payment | null> {
  try {
    const response = await apiClient.get<Payment>(`/payments/api/payments/${orderId}`);
    return response.data;
  } catch (error) {
    if (error instanceof ApiError && error.kind === "notFound") {
      return null;
    }

    throw error;
  }
}

export async function getDeliveryForOrder(orderId: string): Promise<Delivery | null> {
  try {
    const response = await apiClient.get<Delivery>(`/delivery/api/deliveries/${orderId}`);
    return response.data;
  } catch (error) {
    if (error instanceof ApiError && error.kind === "notFound") {
      return null;
    }

    throw error;
  }
}

export async function getMyOrders(limit = 20): Promise<Order[]> {
  const response = await apiClient.get<Order[]>("/orders/api/orders", {
    params: { limit }
  });
  return response.data;
}

export async function createOrder(request: CreateOrderRequest): Promise<Order> {
  const response = await apiClient.post<Order>("/orders/api/orders", request, {
    headers: request.idempotencyKey ? { "Idempotency-Key": request.idempotencyKey } : undefined
  });
  return response.data;
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  const response = await anonymousApiClient.post<AuthResponse>("/identity/api/auth/login", { email, password });
  return response.data;
}

export async function register(email: string, fullName: string, password: string): Promise<AuthResponse> {
  const response = await anonymousApiClient.post<AuthResponse>("/identity/api/auth/register", {
    email,
    fullName,
    password
  });
  return response.data;
}

export async function refreshSession(refreshToken: string): Promise<AuthResponse> {
  const response = await anonymousApiClient.post<AuthResponse>("/identity/api/auth/refresh", { refreshToken });
  return response.data;
}

export async function logout(refreshToken: string): Promise<void> {
  await anonymousApiClient.post("/identity/api/auth/logout", { refreshToken });
}

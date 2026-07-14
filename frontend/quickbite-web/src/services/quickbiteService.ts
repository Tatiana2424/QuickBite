import { anonymousApiClient, apiClient } from "../lib/api";
import { ApiError } from "../lib/apiErrors";
import type {
  AuthResponse,
  CreateOrderRequest,
  CourierDeliveryStatus,
  Delivery,
  MenuItem,
  MenuItemMutationRequest,
  Order,
  OrderSummary,
  OrderSummaryPage,
  Payment,
  RestaurantDetails,
  RestaurantMutationRequest,
  RestaurantSummary
} from "../models";

export async function getRestaurants(): Promise<RestaurantSummary[]> {
  const response = await apiClient.get<RestaurantSummary[]>("/catalog/api/restaurants");
  return response.data;
}

export async function getRestaurantDetails(restaurantId: string): Promise<RestaurantDetails> {
  const response = await apiClient.get<RestaurantDetails>(`/catalog/api/restaurants/${restaurantId}`);
  return response.data;
}

export async function createRestaurant(request: RestaurantMutationRequest): Promise<RestaurantDetails> {
  const response = await apiClient.post<RestaurantDetails>("/catalog/api/restaurants", request);
  return response.data;
}

export async function updateRestaurant(restaurantId: string, request: RestaurantMutationRequest): Promise<RestaurantDetails> {
  const response = await apiClient.put<RestaurantDetails>(`/catalog/api/restaurants/${restaurantId}`, request);
  return response.data;
}

export async function createMenuItem(restaurantId: string, request: MenuItemMutationRequest): Promise<MenuItem> {
  const response = await apiClient.post<MenuItem>(`/catalog/api/restaurants/${restaurantId}/menu`, request);
  return response.data;
}

export async function updateMenuItem(restaurantId: string, menuItemId: string, request: MenuItemMutationRequest): Promise<MenuItem> {
  const response = await apiClient.put<MenuItem>(`/catalog/api/restaurants/${restaurantId}/menu/${menuItemId}`, request);
  return response.data;
}

export async function getOrder(orderId: string): Promise<Order> {
  const response = await apiClient.get<Order>(`/orders/api/orders/my/${orderId}`);
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

export async function getMyCourierDeliveries(): Promise<Delivery[]> {
  const response = await apiClient.get<Delivery[]>("/delivery/api/deliveries/courier/my");
  return response.data;
}

export async function updateCourierDeliveryStatus(deliveryId: string, status: CourierDeliveryStatus): Promise<Delivery> {
  const response = await apiClient.patch<Delivery>(`/delivery/api/deliveries/courier/my/${deliveryId}/status`, { status });
  return response.data;
}

export async function getMyOrders(limit = 20, cursor?: string | null): Promise<OrderSummary[]> {
  const response = await apiClient.get<OrderSummaryPage>("/orders/api/orders/my", {
    params: { limit, cursor }
  });
  return response.data.items;
}

export async function getMyOrdersPage(limit = 20, cursor?: string | null): Promise<OrderSummaryPage> {
  const response = await apiClient.get<OrderSummaryPage>("/orders/api/orders/my", {
    params: { limit, cursor }
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

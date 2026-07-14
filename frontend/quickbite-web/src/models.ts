export interface RestaurantSummary {
  id: string;
  name: string;
  cuisine: string;
  description: string;
}

export interface MenuItem {
  id: string;
  restaurantId: string;
  name: string;
  description: string;
  price: number;
}

export interface RestaurantDetails extends RestaurantSummary {
  menuItems: MenuItem[];
}

export interface OrderItem {
  menuItemId: string;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateOrderItemRequest {
  menuItemId: string;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateOrderRequest {
  userId: string;
  items: CreateOrderItemRequest[];
  idempotencyKey?: string;
}

export interface Order {
  id: string;
  userId: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
  items: OrderItem[];
}

export interface OrderSummary {
  id: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
  itemCount: number;
  itemSummary: string;
}

export interface OrderSummaryPage {
  items: OrderSummary[];
  nextCursor?: string | null;
}

export interface Payment {
  id: string;
  orderId: string;
  amount: number;
  status: string;
  failureReason?: string | null;
}

export interface Delivery {
  id: string;
  orderId: string;
  status: string;
  courierId: string;
  courierName: string;
  courierPhoneNumber: string;
}

export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface AuthSession {
  user: AuthUser;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

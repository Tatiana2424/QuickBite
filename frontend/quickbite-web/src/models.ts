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

export interface DeliveryAddress {
  line1: string;
  line2?: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CreateOrderRequest {
  items: CreateOrderItemRequest[];
  deliveryAddress: DeliveryAddress;
  idempotencyKey?: string;
}

export interface Order {
  id: string;
  userId: string;
  status: string;
  totalAmount: number;
  createdAtUtc: string;
  deliveryAddress: DeliveryAddress;
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
  address: DeliveryAddress;
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

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

export interface Order {
  id: string;
  userId: string;
  status: string;
  totalAmount: number;
  items: OrderItem[];
}

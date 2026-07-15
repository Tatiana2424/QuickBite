import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import { createOrder } from "../src/services/quickbiteService";

const { testAddress } = vi.hoisted(() => ({
  testAddress: {
    line1: "123 Market Street",
    line2: null,
    city: "Seattle",
    state: "WA",
    postalCode: "98101",
    country: "USA"
  }
}));

vi.mock("../src/services/quickbiteService", () => ({
  getRestaurants: vi.fn().mockResolvedValue([
    {
      id: "restaurant-1",
      name: "Urban Bowl",
      cuisine: "Healthy",
      description: "Balanced bowls and fresh wraps."
    },
    {
      id: "restaurant-2",
      name: "Pizza Port",
      cuisine: "Italian",
      description: "Stone baked pizzas and sides."
    },
    {
      id: "restaurant-3",
      name: "Taco Lane",
      cuisine: "Mexican",
      description: "Street tacos, bowls, and bright salsas."
    }
  ]),
  getRestaurantDetails: vi.fn().mockResolvedValue({
    id: "restaurant-1",
    name: "Urban Bowl",
    cuisine: "Healthy",
    description: "Balanced bowls and fresh wraps.",
    menuItems: [
      {
        id: "menu-item-1",
        restaurantId: "restaurant-1",
        name: "Harvest Bowl",
        description: "Grains, greens, and citrus dressing.",
        price: 6.25
      }
    ]
  }),
  getOrder: vi.fn().mockResolvedValue({
    id: "order-1",
    userId: "user-1",
    status: "Confirmed",
    totalAmount: 12.5,
    createdAtUtc: "2026-07-14T12:00:00Z",
    deliveryAddress: testAddress,
    items: [
      {
        menuItemId: "menu-item-1",
        name: "Harvest Bowl",
        quantity: 2,
        unitPrice: 6.25
      }
    ]
  }),
  getPaymentForOrder: vi.fn().mockResolvedValue({
    id: "payment-1",
    orderId: "order-1",
    amount: 12.5,
    status: "Succeeded",
    failureReason: null
  }),
  getDeliveryForOrder: vi.fn().mockResolvedValue({
    id: "delivery-1",
    orderId: "order-1",
    status: "Assigned",
    courierId: "courier-1",
    courierName: "Mia Brooks",
    courierPhoneNumber: "+1-555-0102",
    address: testAddress
  }),
  getMyCourierDeliveries: vi.fn().mockResolvedValue([
    {
      id: "delivery-1",
      orderId: "order-1",
      status: "Assigned",
      courierId: "courier-1",
      courierName: "Alex Rider",
      courierPhoneNumber: "+1-555-0101",
      address: testAddress
    }
  ]),
  updateCourierDeliveryStatus: vi.fn().mockResolvedValue({
    id: "delivery-1",
    orderId: "order-1",
    status: "Accepted",
    courierId: "courier-1",
    courierName: "Alex Rider",
    courierPhoneNumber: "+1-555-0101",
    address: testAddress
  }),
  createRestaurant: vi.fn().mockResolvedValue({
    id: "restaurant-4",
    name: "Noodle House",
    cuisine: "Asian",
    description: "Fresh noodles and broths.",
    menuItems: []
  }),
  updateRestaurant: vi.fn().mockResolvedValue({
    id: "restaurant-1",
    name: "Urban Bowl",
    cuisine: "Healthy",
    description: "Balanced bowls and fresh wraps.",
    menuItems: []
  }),
  createMenuItem: vi.fn().mockResolvedValue({
    id: "menu-item-2",
    restaurantId: "restaurant-1",
    name: "Citrus Salad",
    description: "Greens and bright dressing.",
    price: 8.5
  }),
  updateMenuItem: vi.fn().mockResolvedValue({
    id: "menu-item-1",
    restaurantId: "restaurant-1",
    name: "Harvest Bowl",
    description: "Grains, greens, and citrus dressing.",
    price: 6.25
  }),
  getMyOrders: vi.fn().mockResolvedValue([
    {
      id: "order-1",
      status: "Created",
      totalAmount: 12.5,
      createdAtUtc: "2026-07-14T12:00:00Z",
      itemCount: 2,
      itemSummary: "2 x Harvest Bowl"
    }
  ]),
  createOrder: vi.fn().mockResolvedValue({
    id: "order-1",
    userId: "user-1",
    status: "PaymentProcessing",
    totalAmount: 12.5,
    createdAtUtc: "2026-07-14T12:00:00Z",
    deliveryAddress: testAddress,
    items: [
      {
        menuItemId: "menu-item-1",
        name: "Harvest Bowl",
        quantity: 2,
        unitPrice: 6.25
      }
    ]
  }),
  login: vi.fn().mockResolvedValue({
    userId: "user-1",
    email: "customer@quickbite.local",
    fullName: "QuickBite Customer",
    roles: ["Customer"],
    accessToken: "token",
    refreshToken: "refresh",
    accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
    refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z"
  }),
  register: vi.fn().mockResolvedValue({
    userId: "user-2",
    email: "new.customer@quickbite.local",
    fullName: "New Customer",
    roles: ["Customer"],
    accessToken: "token",
    refreshToken: "refresh",
    accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
    refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z"
  }),
  refreshSession: vi.fn(),
  logout: vi.fn()
}));

afterEach(() => {
  cleanup();
  localStorage.clear();
});

describe("QuickBite app shell", () => {
  it("renders the customer home page separately from restaurant browsing", async () => {
    renderApp("/");

    const primaryNavigation = screen.getByRole("navigation", { name: "Primary navigation" });
    expect(within(primaryNavigation).getByRole("link", { name: "Home" })).toBeTruthy();
    expect(within(primaryNavigation).getByRole("link", { name: "Restaurants" })).toBeTruthy();
    expect(within(primaryNavigation).getByRole("link", { name: "Cart" })).toBeTruthy();
    expect(within(primaryNavigation).getByRole("link", { name: "Orders" })).toBeTruthy();
    expect(await screen.findByRole("heading", { name: "Order something good without the guesswork." })).toBeTruthy();
    expect(screen.getByRole("link", { name: "Browse restaurants" })).toBeTruthy();
    expect((await screen.findAllByText("Urban Bowl")).length).toBeGreaterThan(0);
  });

  it("shows customer footer navigation", async () => {
    renderApp("/");

    const footer = screen.getByRole("contentinfo");
    expect(within(footer).getByLabelText("QuickBite footer").textContent).toContain("Order dinner");
    expect(within(footer).getByRole("navigation", { name: "Footer navigation" })).toBeTruthy();
    expect(within(footer).getByRole("navigation", { name: "Support links" })).toBeTruthy();
    expect(within(footer).getByRole("link", { name: "Create account" })).toBeTruthy();
  });

  it("keeps the restaurants page focused on catalog search and filters", async () => {
    renderApp("/restaurants");

    expect(await screen.findByRole("heading", { name: "Browse restaurants" })).toBeTruthy();
    expect(screen.queryByRole("heading", { name: "Order something good without the guesswork." })).toBeNull();
    expect(await screen.findByRole("region", { name: "All restaurants" })).toBeTruthy();
  });

  it("filters restaurants by search text and cuisine chips", async () => {
    const user = userEvent.setup();
    renderApp("/restaurants");

    const discovery = await screen.findByRole("region", { name: "All restaurants" });
    expect(within(discovery).getByRole("link", { name: /Urban Bowl/ })).toBeTruthy();
    expect(within(discovery).getByRole("link", { name: /Pizza Port/ })).toBeTruthy();

    await user.type(within(discovery).getByLabelText("Search restaurants"), "pizza");

    expect(within(discovery).getByRole("link", { name: /Pizza Port/ })).toBeTruthy();
    expect(within(discovery).queryByRole("link", { name: /Urban Bowl/ })).toBeNull();

    await user.clear(within(discovery).getByLabelText("Search restaurants"));
    await user.click(within(discovery).getByRole("button", { name: "Healthy" }));

    expect(within(discovery).getByRole("link", { name: /Urban Bowl/ })).toBeTruthy();
    expect(within(discovery).queryByRole("link", { name: /Pizza Port/ })).toBeNull();
  });

  it("lets customers save favorite restaurants and see them on home", async () => {
    const user = userEvent.setup();
    renderApp("/restaurants");

    await user.click((await screen.findAllByRole("button", { name: "Save favorite" }))[0]);

    expect(screen.getAllByRole("button", { name: "Saved favorite" }).length).toBeGreaterThan(0);

    await user.click(within(screen.getByRole("navigation", { name: "Primary navigation" })).getByRole("link", { name: "Home" }));

    const favorites = await screen.findByRole("region", { name: "Favorite restaurants" });
    expect(within(favorites).getByRole("link", { name: /Urban Bowl/ })).toBeTruthy();
  });

  it("lets users navigate to the orders page without a page reload", async () => {
    const user = userEvent.setup();
    seedSession();
    renderApp("/");

    await user.click(within(screen.getByRole("navigation", { name: "Primary navigation" })).getByRole("link", { name: "Orders", exact: true }));

    expect(await screen.findByRole("heading", { name: "My Orders" })).toBeTruthy();
    expect(screen.getByText("2 x Harvest Bowl")).toBeTruthy();
  });

  it("shows customer navigation states for signed-out and signed-in users", async () => {
    const { unmount } = renderApp("/");

    expect(screen.getAllByRole("link", { name: "Sign in" }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "Create account" }).length).toBeGreaterThan(0);
    expect(screen.queryByRole("link", { name: "Account" })).toBeNull();

    unmount();
    cleanup();
    seedSession();
    renderApp("/");

    expect(screen.getAllByRole("link", { name: "Account" }).length).toBeGreaterThan(0);
    expect(screen.getByLabelText("Signed in user").textContent).toContain("QuickBite Customer");
  });

  it("opens and closes the mobile navigation menu", async () => {
    const user = userEvent.setup();
    renderApp("/");

    const menuButton = screen.getByRole("button", { name: "Menu" });
    expect(menuButton.getAttribute("aria-expanded")).toBe("false");

    await user.click(menuButton);
    expect(menuButton.getAttribute("aria-expanded")).toBe("true");

    await user.click(within(screen.getByRole("navigation", { name: "Primary navigation" })).getByRole("link", { name: "Cart" }));
    expect(await screen.findByRole("heading", { name: "Your cart" })).toBeTruthy();
    expect(menuButton.getAttribute("aria-expanded")).toBe("false");
  });

  it("shows account details for signed-in customers", async () => {
    seedSession();
    renderApp("/account");

    expect(await screen.findByRole("heading", { name: "Your QuickBite profile" })).toBeTruthy();
    expect(screen.getByText("customer@quickbite.local")).toBeTruthy();
    expect(screen.getByText("Customer")).toBeTruthy();
  });

  it("shows restaurant admin tools for restaurant admins", async () => {
    seedSession(["RestaurantAdmin"]);
    renderApp("/admin/restaurants");

    expect(await screen.findByRole("heading", { name: "Manage restaurants and menus" })).toBeTruthy();
    expect(await screen.findByRole("button", { name: /Urban Bowl/ })).toBeTruthy();
    expect(await screen.findByLabelText("Menu manager")).toBeTruthy();
  });

  it("shows assigned deliveries for couriers", async () => {
    seedSession(["Courier"], "Alex Rider", "courier@quickbite.local");
    renderApp("/courier/deliveries");

    expect(await screen.findByRole("heading", { name: "Assigned deliveries" })).toBeTruthy();
    expect(await screen.findByText("123 Market Street")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Accept" })).toBeTruthy();
  });

  it("guards the account page until the user signs in", async () => {
    renderApp("/account");

    expect(await screen.findByRole("heading", { name: "Sign in to order faster" })).toBeTruthy();
  });

  it("guards order routes until the user signs in", async () => {
    renderApp("/orders");

    expect(await screen.findByRole("heading", { name: "Sign in to order faster" })).toBeTruthy();
  });

  it("lets anonymous users navigate from login to account creation", async () => {
    const user = userEvent.setup();
    renderApp("/login");

    await user.click(await screen.findByRole("link", { name: "Create an account" }));

    expect(await screen.findByRole("heading", { name: "Create your QuickBite account" })).toBeTruthy();
  });

  it("signs users in after successful account creation", async () => {
    const user = userEvent.setup();
    renderApp("/register");

    await user.type(await screen.findByLabelText("Full name"), "New Customer");
    await user.type(screen.getByLabelText("Email"), "new.customer@quickbite.local");
    await user.type(screen.getByLabelText("Password"), "Pass123!");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByRole("heading", { name: "My Orders" })).toBeTruthy();
    expect(screen.getByText("New Customer")).toBeTruthy();
  });

  it("lets signed-in customers add menu items and checkout from a dedicated page", async () => {
    const user = userEvent.setup();
    seedSession();
    renderApp("/restaurants/restaurant-1");

    await user.click(await screen.findByRole("button", { name: "Add to cart" }));
    await user.click(screen.getByRole("button", { name: "Increase Harvest Bowl" }));

    expect(screen.getAllByText("$12.50")).toHaveLength(2);
    expect(screen.queryByRole("heading", { name: "Where should we bring it?" })).toBeNull();

    await user.click(screen.getByRole("button", { name: "Review cart" }));
    expect(await screen.findByRole("heading", { name: "Your cart" })).toBeTruthy();

    await user.click(screen.getByRole("button", { name: "Continue checkout" }));
    expect(await screen.findByRole("heading", { name: "Checkout" })).toBeTruthy();
    expect(screen.getByLabelText("Checkout order summary").textContent).toContain("$12.50");

    await user.click(screen.getByRole("button", { name: "Place order" }));

    expect(await screen.findByRole("heading", { name: "Order order-1" })).toBeTruthy();
    expect(createOrder).toHaveBeenCalledWith(expect.objectContaining({
      deliveryAddress: testAddress,
      items: [
        {
          menuItemId: "menu-item-1",
          name: "Harvest Bowl",
          quantity: 2,
          unitPrice: 6.25
        }
      ]
    }));
  });

  it("lets customers review cart quantities before checkout", async () => {
    const user = userEvent.setup();
    seedSession();
    seedCart();
    renderApp("/cart");

    expect(await screen.findByRole("heading", { name: "Your cart" })).toBeTruthy();
    expect(screen.getByText("Harvest Bowl")).toBeTruthy();
    expect(screen.getAllByText("$12.50").length).toBeGreaterThan(0);

    await user.click(screen.getByRole("button", { name: "Increase Harvest Bowl" }));
    expect(screen.getAllByText("$18.75").length).toBeGreaterThan(0);

    await user.click(screen.getByRole("button", { name: "Continue checkout" }));
    expect(await screen.findByRole("heading", { name: "Checkout" })).toBeTruthy();
    expect(screen.getByLabelText("Checkout order summary").textContent).toContain("$18.75");
  });

  it("guides signed-in customers away from checkout when the cart is empty", async () => {
    seedSession();
    renderApp("/checkout");

    expect(await screen.findByRole("heading", { name: "Checkout" })).toBeTruthy();
    expect(screen.getByText("Your cart is empty")).toBeTruthy();
    expect(screen.getByRole("link", { name: "Browse restaurants" })).toBeTruthy();
  });

  it("shows payment status and pending delivery on order details", async () => {
    seedSession();
    renderApp("/orders/order-1");

    expect(await screen.findByRole("heading", { name: "Order order-1" })).toBeTruthy();
    expect(await screen.findByRole("region", { name: "Order progress" })).toBeTruthy();
    expect((await screen.findAllByText("Succeeded")).length).toBeGreaterThan(0);
    expect(await screen.findByText("Payment succeeded and the kitchen can continue.")).toBeTruthy();
    expect(await screen.findByText("Mia Brooks")).toBeTruthy();
    expect(screen.getAllByText(/123 Market Street/).length).toBeGreaterThan(0);
    expect(screen.getByText("2 x Harvest Bowl")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Refresh status" })).toBeTruthy();
  });

  it("lets signed-in customers reorder a previous order into the cart", async () => {
    const user = userEvent.setup();
    seedSession();
    renderApp("/orders/order-1");

    await user.click(await screen.findByRole("button", { name: "Reorder" }));

    expect(await screen.findByRole("heading", { name: "Your cart" })).toBeTruthy();
    expect(screen.getByText(/Reorder started/)).toBeTruthy();
    expect(screen.getByText("Harvest Bowl")).toBeTruthy();
    expect(screen.getAllByText("$12.50").length).toBeGreaterThan(0);
  });

  it("prefills the login form with the seeded customer account", async () => {
    renderApp("/login");

    expect(await screen.findByDisplayValue("customer@quickbite.local")).toBeTruthy();
  });
});

function renderApp(initialRoute: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter initialEntries={[initialRoute]}>
          <App />
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}


function seedSession(roles = ["Customer"], fullName = "QuickBite Customer", email = "customer@quickbite.local") {
  localStorage.setItem(
    "quickbite.auth.session",
    JSON.stringify({
      user: {
        id: "user-1",
        email,
        fullName,
        roles
      },
      accessToken: "token",
      accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
      refreshToken: "refresh",
      refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z"
    })
  );
}

function seedCart() {
  localStorage.setItem(
    "quickbite.cart",
    JSON.stringify([
      {
        menuItemId: "menu-item-1",
        restaurantId: "restaurant-1",
        name: "Harvest Bowl",
        unitPrice: 6.25,
        quantity: 2
      }
    ])
  );
}

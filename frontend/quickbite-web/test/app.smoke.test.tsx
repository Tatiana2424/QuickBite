import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";
import { createOrder } from "../src/services/quickbiteService";

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
  getDeliveryForOrder: vi.fn().mockResolvedValue(null),
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
  it("renders navigation and restaurant data from the gateway service layer", async () => {
    renderApp("/");

    expect(screen.getByRole("link", { name: "Restaurants" })).toBeTruthy();
    expect(screen.getByRole("link", { name: "My orders" })).toBeTruthy();
    expect(await screen.findByRole("heading", { name: "Find the right meal, then checkout fast." })).toBeTruthy();
    expect((await screen.findAllByText("Urban Bowl")).length).toBeGreaterThan(0);
  });

  it("filters restaurants by search text and cuisine chips", async () => {
    const user = userEvent.setup();
    renderApp("/");

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

  it("lets users navigate to the orders page without a page reload", async () => {
    const user = userEvent.setup();
    seedSession();
    renderApp("/");

    await user.click(screen.getByRole("link", { name: "My orders" }));

    expect(await screen.findByRole("heading", { name: "My Orders" })).toBeTruthy();
    expect(screen.getByText("2 x Harvest Bowl")).toBeTruthy();
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

  it("lets signed-in customers add menu items and checkout", async () => {
    const user = userEvent.setup();
    seedSession();
    renderApp("/restaurants/restaurant-1");

    await user.click(await screen.findByRole("button", { name: "Add to cart" }));
    await user.click(screen.getByRole("button", { name: "Increase Harvest Bowl" }));

    expect(screen.getAllByText("$12.50")).toHaveLength(2);

    await user.click(screen.getByRole("button", { name: "Checkout" }));

    expect(await screen.findByRole("heading", { name: "Order order-1" })).toBeTruthy();
    expect(createOrder).toHaveBeenCalledWith(expect.objectContaining({
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

  it("shows payment status and pending delivery on order details", async () => {
    seedSession();
    renderApp("/orders/order-1");

    expect(await screen.findByRole("heading", { name: "Order order-1" })).toBeTruthy();
    expect(await screen.findByText("Succeeded")).toBeTruthy();
    expect(await screen.findByText("Delivery will be assigned after payment confirmation.")).toBeTruthy();
    expect(screen.getByText("2 x Harvest Bowl")).toBeTruthy();
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

  render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter initialEntries={[initialRoute]}>
          <App />
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}

function seedSession() {
  localStorage.setItem(
    "quickbite.auth.session",
    JSON.stringify({
      user: {
        id: "user-1",
        email: "customer@quickbite.local",
        fullName: "QuickBite Customer",
        roles: ["Customer"]
      },
      accessToken: "token",
      accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
      refreshToken: "refresh",
      refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z"
    })
  );
}

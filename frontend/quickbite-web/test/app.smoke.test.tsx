import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthProvider } from "../src/auth/AuthContext";

vi.mock("../src/services/quickbiteService", () => ({
  getRestaurants: vi.fn().mockResolvedValue([
    {
      id: "restaurant-1",
      name: "Urban Bowl",
      cuisine: "Healthy",
      description: "Balanced bowls and fresh wraps."
    }
  ]),
  getRestaurantDetails: vi.fn().mockResolvedValue({
    id: "restaurant-1",
    name: "Urban Bowl",
    cuisine: "Healthy",
    description: "Balanced bowls and fresh wraps.",
    menuItems: []
  }),
  getOrder: vi.fn().mockResolvedValue({
    id: "order-1",
    userId: "user-1",
    status: "Created",
    totalAmount: 12.5,
    items: []
  }),
  login: vi.fn().mockResolvedValue({
    userId: "user-1",
    email: "demo@quickbite.local",
    fullName: "Demo Customer",
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
    expect(screen.getByRole("link", { name: "Orders" })).toBeTruthy();
    expect(await screen.findByText("Urban Bowl")).toBeTruthy();
  });

  it("lets users navigate to the orders page without a page reload", async () => {
    const user = userEvent.setup();
    seedSession();
    renderApp("/");

    await user.click(screen.getByRole("link", { name: "Orders" }));

    expect(await screen.findByRole("heading", { name: "Order lookup" })).toBeTruthy();
  });

  it("guards order routes until the user signs in", async () => {
    renderApp("/orders");

    expect(await screen.findByRole("heading", { name: "Sign in to QuickBite" })).toBeTruthy();
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
        email: "demo@quickbite.local",
        fullName: "Demo Customer",
        roles: ["Customer"]
      },
      accessToken: "token",
      accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
      refreshToken: "refresh",
      refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z"
    })
  );
}

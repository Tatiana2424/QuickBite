import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";

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
    accessToken: "token",
    refreshToken: "refresh",
    expiresAtUtc: "2026-05-02T00:00:00Z"
  })
}));

afterEach(() => {
  cleanup();
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
    renderApp("/");

    await user.click(screen.getByRole("link", { name: "Orders" }));

    expect(await screen.findByRole("heading", { name: "Order lookup" })).toBeTruthy();
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
      <MemoryRouter initialEntries={[initialRoute]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

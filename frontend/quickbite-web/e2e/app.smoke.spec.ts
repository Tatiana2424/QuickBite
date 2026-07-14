import { expect, test } from "@playwright/test";

test.beforeEach(async ({ page }) => {
  await page.route("**/catalog/api/restaurants", async (route) => {
    await route.fulfill({
      json: [
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
      ]
    });
  });

  await page.route("**/identity/api/auth/login", async (route) => {
    await route.fulfill({
      json: {
        userId: "user-1",
        email: "demo@quickbite.local",
        fullName: "Demo Customer",
        roles: ["Customer"],
        accessToken: "access-token",
        accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
        refreshToken: "refresh-token",
        refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z"
      }
    });
  });

  await page.route("**/catalog/api/restaurants/restaurant-1", async (route) => {
    await route.fulfill({
      json: {
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
      }
    });
  });

  await page.route("**/orders/api/orders/my?**", async (route) => {
    await route.fulfill({
      json: {
        items: [
          {
            id: "order-1",
            status: "Created",
            totalAmount: 12.5,
            createdAtUtc: "2026-07-14T12:00:00Z",
            itemCount: 2,
            itemSummary: "2 x Harvest Bowl"
          }
        ],
        nextCursor: null
      }
    });
  });

  await page.route("**/orders/api/orders", async (route) => {
    if (route.request().method() !== "POST") {
      await route.fallback();
      return;
    }

    const requestBody = route.request().postDataJSON() as Record<string, unknown>;
    expect(requestBody.userId).toBeUndefined();

    await route.fulfill({
      json: {
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
      }
    });
  });

  await page.route("**/orders/api/orders/my/order-1", async (route) => {
    await route.fulfill({
      json: {
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
      }
    });
  });

  await page.route("**/payments/api/payments/order-1", async (route) => {
    await route.fulfill({
      json: {
        id: "payment-1",
        orderId: "order-1",
        amount: 12.5,
        status: "Succeeded",
        failureReason: null
      }
    });
  });

  await page.route("**/delivery/api/deliveries/order-1", async (route) => {
    await route.fulfill({
      status: 404,
      json: {
        title: "Resource not found.",
        detail: "Delivery for order 'order-1' was not found."
      }
    });
  });
});

test("loads restaurants through the gateway contract", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "Find the right meal, then checkout fast." })).toBeVisible();
  await expect(page.getByRole("link", { name: /Urban Bowl/ }).first()).toBeVisible();
});

test("filters restaurant discovery by search and cuisine", async ({ page }) => {
  await page.goto("/");

  const discovery = page.getByRole("region", { name: "All restaurants" });
  await expect(discovery.getByRole("link", { name: /Urban Bowl/ })).toBeVisible();
  await expect(discovery.getByRole("link", { name: /Pizza Port/ })).toBeVisible();

  await discovery.getByLabel("Search restaurants").fill("pizza");

  await expect(discovery.getByRole("link", { name: /Pizza Port/ })).toBeVisible();
  await expect(discovery.getByRole("link", { name: /Urban Bowl/ })).toHaveCount(0);

  await discovery.getByLabel("Search restaurants").fill("");
  await discovery.getByRole("button", { name: "Healthy" }).click();

  await expect(discovery.getByRole("link", { name: /Urban Bowl/ })).toBeVisible();
  await expect(discovery.getByRole("link", { name: /Pizza Port/ })).toHaveCount(0);
});

test("guards orders and returns after login", async ({ page }) => {
  await page.goto("/orders");

  await expect(page.getByRole("heading", { name: "Sign in to order faster" })).toBeVisible();
  await page.getByRole("button", { name: "Sign in" }).click();

  await expect(page.getByRole("heading", { name: "My Orders" })).toBeVisible();
  await expect(page.getByText("2 x Harvest Bowl")).toBeVisible();
  await expect(page.getByLabel("Signed in user")).toContainText("Demo Customer");
});

test("lets signed-in customers add menu items and checkout", async ({ page }) => {
  await page.goto("/login");
  await page.getByRole("button", { name: "Sign in" }).click();

  await page.goto("/restaurants/restaurant-1");
  await page.getByRole("button", { name: "Add to cart" }).click();
  await page.getByRole("button", { name: "Increase Harvest Bowl" }).click();

  await expect(page.getByLabel("Cart summary")).toContainText("$12.50");
  await page.getByRole("button", { name: "Checkout" }).click();

  await expect(page.getByRole("heading", { name: "Order order-1" })).toBeVisible();
  await expect(page.getByText("Succeeded")).toBeVisible();
  await expect(page.getByText("Delivery will be assigned after payment confirmation.")).toBeVisible();
});

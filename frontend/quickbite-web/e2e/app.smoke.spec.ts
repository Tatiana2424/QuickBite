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

  await page.route("**/orders/api/orders?**", async (route) => {
    await route.fulfill({
      json: [
        {
          id: "order-1",
          userId: "user-1",
          status: "Created",
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
      ]
    });
  });

  await page.route("**/orders/api/orders", async (route) => {
    if (route.request().method() !== "POST") {
      await route.fallback();
      return;
    }

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

  await page.route("**/orders/api/orders/order-1", async (route) => {
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
});

test("loads restaurants through the gateway contract", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "Restaurants" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Urban Bowl" })).toBeVisible();
});

test("guards orders and returns after login", async ({ page }) => {
  await page.goto("/orders");

  await expect(page.getByRole("heading", { name: "Sign in to QuickBite" })).toBeVisible();
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
});

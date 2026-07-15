import { expect, test } from "@playwright/test";

const testAddress = {
  line1: "123 Market Street",
  line2: null,
  city: "Seattle",
  state: "WA",
  postalCode: "98101",
  country: "USA"
};

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
    const requestBody = route.request().postDataJSON() as Record<string, string>;
    if (requestBody.email === "wrong@quickbite.local") {
      await route.fulfill({
        status: 401,
        json: {
          title: "Unauthorized",
          detail: "Invalid credentials.",
          status: 401,
          traceId: "trace-invalid-login"
        }
      });
      return;
    }

    await route.fulfill({
      json: {
        userId: "user-1",
        email: requestBody.email || "customer@quickbite.local",
        fullName: "Demo Customer",
        roles: ["Customer"],
        accessToken: "access-token",
        accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
        refreshToken: "refresh-token",
        refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z"
      }
    });
  });

  await page.route("**/identity/api/auth/register", async (route) => {
    const requestBody = route.request().postDataJSON() as Record<string, string>;
    await route.fulfill({
      json: {
        userId: "registered-user-1",
        email: requestBody.email,
        fullName: requestBody.fullName,
        roles: ["Customer"],
        accessToken: "registered-access-token",
        accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
        refreshToken: "registered-refresh-token",
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
    expect(requestBody.deliveryAddress).toEqual(testAddress);

    await route.fulfill({
      json: {
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
        deliveryAddress: testAddress,
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
      json: {
        id: "delivery-1",
        orderId: "order-1",
        status: "Assigned",
        courierId: "courier-1",
        courierName: "Mia Brooks",
        courierPhoneNumber: "+1-555-0102",
        address: testAddress
      }
    });
  });
});

test("loads the customer home page through the gateway contract", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "Order something good without the guesswork." })).toBeVisible();
  await expect(page.getByRole("link", { name: "Browse restaurants" })).toBeVisible();
  await expect(page.getByRole("link", { name: /Urban Bowl/ }).first()).toBeVisible();
  await expect(page.getByRole("contentinfo")).toContainText("Order dinner");
  await expect(page.getByRole("navigation", { name: "Footer navigation" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Support links" })).toBeVisible();
});

test("shows restaurant catalog content separately from home", async ({ page }) => {
  await page.goto("/restaurants");

  await expect(page.getByRole("heading", { name: "Browse restaurants" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Order something good without the guesswork." })).toHaveCount(0);
  await expect(page.getByRole("region", { name: "All restaurants" })).toBeVisible();
});

test("filters restaurant discovery by search and cuisine", async ({ page }) => {
  await page.goto("/restaurants");

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

test("shows account information for signed-in customers", async ({ page }) => {
  await page.goto("/login");
  await page.getByRole("button", { name: "Sign in" }).click();

  await page.getByRole("navigation", { name: "Primary navigation" }).getByRole("link", { name: "Account" }).click();

  await expect(page.getByRole("heading", { name: "Your QuickBite profile" })).toBeVisible();
  await expect(page.getByLabel("Account information")).toContainText("customer@quickbite.local");
  await expect(page.getByLabel("Account information")).toContainText("Customer");
});

test("registers a customer account and lands on protected orders", async ({ page }) => {
  await page.goto("/register");
  await page.getByLabel("Full name").fill("New Customer");
  await page.getByLabel("Email").fill("new.customer@quickbite.local");
  await page.getByLabel("Password").fill("Pass123!");
  await page.getByRole("button", { name: "Create account" }).click();

  await expect(page.getByRole("heading", { name: "My Orders" })).toBeVisible();
  await expect(page.getByLabel("Signed in user")).toContainText("New Customer");
});

test("shows an error for invalid login credentials", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("Email").fill("wrong@quickbite.local");
  await page.getByLabel("Password").fill("bad-password");
  await page.getByRole("button", { name: "Sign in" }).click();

  await expect(page.getByText("Invalid email or password.")).toBeVisible();
});

test("lets signed-in customers add menu items and checkout from a dedicated page", async ({ page }) => {
  await page.goto("/login");
  await page.getByRole("button", { name: "Sign in" }).click();

  await page.goto("/restaurants/restaurant-1");
  await page.getByRole("button", { name: "Add to cart" }).click();
  await page.getByRole("button", { name: "Increase Harvest Bowl" }).click();

  await expect(page.getByLabel("Cart summary")).toContainText("$12.50");
  await expect(page.getByRole("heading", { name: "Where should we bring it?" })).toHaveCount(0);
  await page.getByRole("button", { name: "Review cart" }).click();

  await expect(page.getByRole("heading", { name: "Your cart" })).toBeVisible();
  await page.getByRole("button", { name: "Continue checkout" }).click();

  await expect(page.getByRole("heading", { name: "Checkout" })).toBeVisible();
  await expect(page.getByLabel("Checkout order summary")).toContainText("$12.50");
  await page.getByRole("button", { name: "Place order" }).click();

  await expect(page.getByRole("heading", { name: "Order order-1" })).toBeVisible();
  await expect(page.getByLabel("Order progress").getByText("Succeeded", { exact: true })).toBeVisible();
  await expect(page.getByText("Payment is complete. The restaurant can start preparing the order.")).toBeVisible();
  await expect(page.getByText("Mia Brooks", { exact: true })).toBeVisible();
  await expect(page.getByText("123 Market Street").first()).toBeVisible();

  await page.getByRole("navigation", { name: "Primary navigation" }).getByRole("link", { name: "Orders", exact: true }).click();
  await expect(page.getByRole("heading", { name: "My Orders" })).toBeVisible();
  await expect(page.getByText("2 x Harvest Bowl")).toBeVisible();
});

test("guides signed-in customers away from checkout when the cart is empty", async ({ page }) => {
  await page.goto("/login");
  await page.getByRole("button", { name: "Sign in" }).click();

  await page.goto("/checkout");

  await expect(page.getByRole("heading", { name: "Checkout" })).toBeVisible();
  await expect(page.getByText("Your cart is empty")).toBeVisible();
  await expect(page.getByRole("link", { name: "Browse restaurants" })).toBeVisible();
});

test("opens the cart route and mobile navigation", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 760 });
  await page.goto("/");

  const menuButton = page.getByRole("button", { name: "Menu" });
  await expect(menuButton).toHaveAttribute("aria-expanded", "false");
  await menuButton.click();
  await expect(menuButton).toHaveAttribute("aria-expanded", "true");
  await page.getByRole("link", { name: "Cart", exact: true }).click();

  await expect(page.getByRole("heading", { name: "Your cart" })).toBeVisible();
  await expect(page.getByText("Your cart is empty")).toBeVisible();
  await expect(menuButton).toHaveAttribute("aria-expanded", "false");
});

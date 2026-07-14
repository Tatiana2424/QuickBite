import { expect, test } from "@playwright/test";

const runFullStack = process.env.QUICKBITE_FULLSTACK_E2E === "1";
const urbanBowlRestaurantId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1";

test.skip(!runFullStack, "Set QUICKBITE_FULLSTACK_E2E=1 and run the local full stack to enable this suite.");

test("completes the real customer registration, checkout, and order visibility flow", async ({ page }) => {
  const email = `customer.${Date.now()}@quickbite.local`;

  await page.goto("/register");
  await page.getByLabel("Full name").fill("Full Stack Customer");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill("Pass123!");
  await page.getByRole("button", { name: "Create account" }).click();

  await expect(page.getByRole("heading", { name: "My Orders" })).toBeVisible();
  await expect(page.getByLabel("Signed in user")).toContainText("Full Stack Customer");

  await page.goto(`/restaurants/${urbanBowlRestaurantId}`);
  await expect(page.getByRole("heading", { name: "Urban Bowl" })).toBeVisible();
  await page.getByRole("button", { name: "Add to cart" }).first().click();
  await page.getByRole("button", { name: /^Checkout$/ }).click();

  await expect(page.getByRole("heading", { name: /^Order / })).toBeVisible();
  await expect(page.getByText("123 Market Street").first()).toBeVisible();
  await expect(page.getByText(/Payment is being prepared\.|Amount: \$|Payment status could not be loaded\./)).toBeVisible();
  await expect(page.getByText(/Delivery will be assigned after payment confirmation\.|Alex Rider|Mia Brooks|Noah Patel|Delivery status could not be loaded\./)).toBeVisible();

  await page.getByRole("link", { name: "Back to orders" }).click();
  await expect(page.getByRole("heading", { name: "My Orders" })).toBeVisible();
  await expect(page.getByText("Chicken Power Bowl")).toBeVisible();
});

test("shows the seeded customer order states against the real gateway", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("Email").fill("customer@quickbite.local");
  await page.getByLabel("Password").fill("Pass123!");
  await page.getByRole("button", { name: "Sign in" }).click();

  await expect(page.getByRole("heading", { name: "My Orders" })).toBeVisible();
  await expect(page.getByText("Confirmed")).toBeVisible();
  await expect(page.getByText("PaymentProcessing")).toBeVisible();
  await expect(page.getByText("Failed")).toBeVisible();
});

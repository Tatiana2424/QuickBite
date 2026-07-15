import { Link, NavLink, Route, Routes, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { useAuth } from "./auth/AuthContext";
import { CartProvider, useCart } from "./cart/CartContext";
import { AccountPage } from "./pages/AccountPage";
import { CartPage } from "./pages/CartPage";
import { CheckoutPage } from "./pages/CheckoutPage";
import { CourierDeliveriesPage } from "./pages/CourierDeliveriesPage";
import { HomePage } from "./pages/HomePage";
import { LoginPage } from "./pages/LoginPage";
import { OrderDetailsPage } from "./pages/OrderDetailsPage";
import { OrdersPage } from "./pages/OrdersPage";
import { RegisterPage } from "./pages/RegisterPage";
import { RestaurantAdminPage } from "./pages/RestaurantAdminPage";
import { RestaurantDetailsPage } from "./pages/RestaurantDetailsPage";
import { RestaurantsPage } from "./pages/RestaurantsPage";
import { SupportPage } from "./pages/SupportPage";

export function App() {
  return (
    <CartProvider>
      <AppShell />
    </CartProvider>
  );
}

function AppShell() {
  const { isAuthenticated, user, logout } = useAuth();
  const { itemCount } = useCart();
  const location = useLocation();
  const [isMobileNavOpen, setIsMobileNavOpen] = useState(false);
  const canManageRestaurants = user?.roles.some((role) => role === "RestaurantAdmin" || role === "PlatformAdmin");
  const canDeliver = user?.roles.some((role) => role === "Courier" || role === "PlatformAdmin");

  useEffect(() => {
    setIsMobileNavOpen(false);
  }, [location.pathname]);

  return (
    <div className="app-shell">
      <header className="site-header">
        <div className="site-header__inner">
          <div className="brand-lockup">
            <BrandMark />
            <div>
              <strong>QuickBite</strong>
              <span>Fresh food, fast checkout</span>
            </div>
          </div>
          <button
            type="button"
            className="mobile-menu-button button-secondary"
            aria-controls="primary-navigation"
            aria-expanded={isMobileNavOpen}
            onClick={() => setIsMobileNavOpen((current) => !current)}
          >
            Menu
          </button>
          <nav
            id="primary-navigation"
            className={isMobileNavOpen ? "primary-nav primary-nav--open" : "primary-nav"}
            aria-label="Primary navigation"
          >
            <NavLink to="/" end>Home</NavLink>
            <NavLink to="/restaurants">Restaurants</NavLink>
            <NavLink to="/#favorites">Favorites</NavLink>
            <NavLink to="/cart" className="cart-nav-link">
              Cart
              {itemCount > 0 && <span className="cart-badge" aria-label={`${itemCount} items in cart`}>{itemCount}</span>}
            </NavLink>
            <NavLink to="/orders">Orders</NavLink>
            {isAuthenticated && <NavLink to="/account">Account</NavLink>}
            {canManageRestaurants && <NavLink to="/admin/restaurants">Admin</NavLink>}
            {canDeliver && <NavLink to="/courier/deliveries">Courier</NavLink>}
          </nav>
          <div className="account-actions">
            {!isAuthenticated && <Link className="button-link" to="/login">Sign in</Link>}
            {!isAuthenticated && <Link className="button-link button-link--primary" to="/register">Create account</Link>}
            {isAuthenticated && user && (
              <section className="session-card" aria-label="Signed in user">
                <span className="session-avatar" aria-hidden="true">{getInitials(user.fullName)}</span>
                <strong>{user.fullName}</strong>
                <button type="button" className="session-sign-out" onClick={() => void logout()}>
                  Sign out
                </button>
              </section>
            )}
          </div>
        </div>
      </header>
      <main className="content">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/restaurants" element={<RestaurantsPage />} />
          <Route path="/restaurants/:restaurantId" element={<RestaurantDetailsPage />} />
          <Route path="/cart" element={<CartPage />} />
          <Route
            path="/checkout"
            element={
              <ProtectedRoute>
                <CheckoutPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/orders"
            element={
              <ProtectedRoute>
                <OrdersPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/orders/:orderId"
            element={
              <ProtectedRoute>
                <OrderDetailsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/account"
            element={
              <ProtectedRoute>
                <AccountPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/admin/restaurants"
            element={
              <ProtectedRoute>
                <RestaurantAdminPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/courier/deliveries"
            element={
              <ProtectedRoute>
                <CourierDeliveriesPage />
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/support" element={<SupportPage />} />
        </Routes>
      </main>
      <Footer isAuthenticated={isAuthenticated} />
    </div>
  );
}

function BrandMark() {
  return (
    <Link to="/" className="brand-mark" aria-label="QuickBite home">
      <svg viewBox="0 0 64 64" role="img" aria-hidden="true" focusable="false">
        <circle className="brand-mark__plate" cx="32" cy="32" r="28" />
        <path className="brand-mark__speed" d="M12 25c-3 1-5 3-6 5M14 18c-4 1-7 3-9 6" />
        <circle className="brand-mark__q-ring" cx="29" cy="32" r="15" />
        <path className="brand-mark__q-tail" d="M39 42l9 9" />
        <path className="brand-mark__cloche" d="M19 41h24M23 38c2-9 15-10 18 0M32 27v-5M28 22h8" />
        <path className="brand-mark__leaf" d="M45 25c7-5 11-2 10 5-6 2-10 1-10-5Z" />
        <path className="brand-mark__leaf-vein" d="M46 28c3-1 5-2 8-4" />
      </svg>
    </Link>
  );
}

function getInitials(fullName: string) {
  return fullName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
}

function Footer({ isAuthenticated }: { isAuthenticated: boolean }) {
  return (
    <footer className="site-footer">
      <div className="site-footer__inner">
        <section className="footer-brand" aria-label="QuickBite footer">
          <BrandMark />
          <div>
            <strong>QuickBite</strong>
            <p>Order dinner, track progress, and keep your favorites close.</p>
          </div>
        </section>
        <nav className="footer-links" aria-label="Explore links">
          <strong>Explore</strong>
          <Link to="/">Home</Link>
          <Link to="/restaurants">Restaurants</Link>
          <Link to="/orders">Orders</Link>
          <Link to={isAuthenticated ? "/account" : "/login"}>{isAuthenticated ? "Account" : "Sign in"}</Link>
          {!isAuthenticated && <Link to="/register">Create account</Link>}
        </nav>
        <nav className="footer-links" aria-label="Help links">
          <strong>Help</strong>
          <Link to="/support">Support center</Link>
          <a href="mailto:partners@quickbite.local">Contact team</a>
          <a href="#privacy">Privacy</a>
          <a href="#terms">Terms</a>
        </nav>
        <section className="footer-support-card" aria-label="Customer help">
          <strong>Need order help?</strong>
          <p>Get payment, delivery, or account help without hunting through your order ids.</p>
          <Link className="button-link button-link--primary" to="/support">Open support</Link>
        </section>
      </div>
    </footer>
  );
}

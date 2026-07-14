import { Link, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { useAuth } from "./auth/AuthContext";
import { CartProvider, useCart } from "./cart/CartContext";
import { AccountPage } from "./pages/AccountPage";
import { CourierDeliveriesPage } from "./pages/CourierDeliveriesPage";
import { LoginPage } from "./pages/LoginPage";
import { OrderDetailsPage } from "./pages/OrderDetailsPage";
import { OrdersPage } from "./pages/OrdersPage";
import { RegisterPage } from "./pages/RegisterPage";
import { RestaurantAdminPage } from "./pages/RestaurantAdminPage";
import { RestaurantDetailsPage } from "./pages/RestaurantDetailsPage";
import { RestaurantsPage } from "./pages/RestaurantsPage";

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
  const canManageRestaurants = user?.roles.some((role) => role === "RestaurantAdmin" || role === "PlatformAdmin");
  const canDeliver = user?.roles.some((role) => role === "Courier" || role === "PlatformAdmin");

  return (
    <div className="app-shell">
      <header className="site-header">
        <div className="brand-lockup">
          <Link to="/" className="brand-mark" aria-label="QuickBite home">QB</Link>
          <div>
            <strong>QuickBite</strong>
            <span>Fresh food, fast checkout</span>
          </div>
        </div>
        <nav aria-label="Primary navigation">
          <Link to="/">Restaurants</Link>
          <Link to="/orders">My orders</Link>
          {canManageRestaurants && <Link to="/admin/restaurants">Admin</Link>}
          {canDeliver && <Link to="/courier/deliveries">Courier</Link>}
          {isAuthenticated && <Link to="/account">Account</Link>}
          {itemCount > 0 && <span className="cart-badge" aria-label={`${itemCount} items in cart`}>{itemCount}</span>}
        </nav>
        <div className="account-actions">
          {!isAuthenticated && <Link className="button-link" to="/login">Sign in</Link>}
          {!isAuthenticated && <Link className="button-link button-link--primary" to="/register">Create account</Link>}
          {isAuthenticated && user && (
            <section className="session-card" aria-label="Signed in user">
              <strong>{user.fullName}</strong>
              <button type="button" className="button-secondary" onClick={() => void logout()}>
                Sign out
              </button>
            </section>
          )}
        </div>
      </header>
      <main className="content">
        <Routes>
          <Route path="/" element={<RestaurantsPage />} />
          <Route path="/restaurants/:restaurantId" element={<RestaurantDetailsPage />} />
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
        </Routes>
      </main>
    </div>
  );
}

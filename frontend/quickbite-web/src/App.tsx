import { Link, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { useAuth } from "./auth/AuthContext";
import { LoginPage } from "./pages/LoginPage";
import { OrdersPage } from "./pages/OrdersPage";
import { RestaurantDetailsPage } from "./pages/RestaurantDetailsPage";
import { RestaurantsPage } from "./pages/RestaurantsPage";

export function App() {
  const { isAuthenticated, user, logout } = useAuth();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">QuickBite</p>
          <h1>Food delivery control room</h1>
          <p className="muted">An event-driven starter for restaurants, payments, and delivery workflows.</p>
        </div>
        <div className="sidebar__footer">
          <nav aria-label="Primary navigation">
            <Link to="/">Restaurants</Link>
            <Link to="/orders">Orders</Link>
            {!isAuthenticated && <Link to="/login">Login</Link>}
          </nav>
          {isAuthenticated && user && (
            <section className="session-card" aria-label="Signed in user">
              <span>Signed in as</span>
              <strong>{user.fullName}</strong>
              <small>{user.roles.join(", ") || "Customer"}</small>
              <button type="button" className="button-secondary" onClick={() => void logout()}>
                Sign out
              </button>
            </section>
          )}
        </div>
      </aside>
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
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </main>
    </div>
  );
}

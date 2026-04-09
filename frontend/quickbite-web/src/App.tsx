import { Link, Route, Routes } from "react-router-dom";
import { LoginPage } from "./pages/LoginPage";
import { OrdersPage } from "./pages/OrdersPage";
import { RestaurantDetailsPage } from "./pages/RestaurantDetailsPage";
import { RestaurantsPage } from "./pages/RestaurantsPage";

export function App() {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">QuickBite</p>
          <h1>Food delivery control room</h1>
          <p className="muted">An event-driven starter for restaurants, payments, and delivery workflows.</p>
        </div>
        <nav>
          <Link to="/">Restaurants</Link>
          <Link to="/orders">Orders</Link>
          <Link to="/login">Login</Link>
        </nav>
      </aside>
      <main className="content">
        <Routes>
          <Route path="/" element={<RestaurantsPage />} />
          <Route path="/restaurants/:restaurantId" element={<RestaurantDetailsPage />} />
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </main>
    </div>
  );
}

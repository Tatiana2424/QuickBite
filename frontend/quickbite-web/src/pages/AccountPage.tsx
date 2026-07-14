import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function AccountPage() {
  const { user, session, logout } = useAuth();
  const navigate = useNavigate();

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  return (
    <section className="stack">
      <div className="page-heading">
        <p className="eyebrow">Account</p>
        <h2>Your QuickBite profile</h2>
        <p className="muted">Manage your customer session today. Addresses and delivery preferences will live here next.</p>
      </div>
      <section className="account-grid" aria-label="Account information">
        <article className="panel account-panel">
          <div>
            <p className="eyebrow">Profile</p>
            <h3>{user?.fullName}</h3>
          </div>
          <dl className="detail-list">
            <div>
              <dt>Email</dt>
              <dd>{user?.email}</dd>
            </div>
            <div>
              <dt>Roles</dt>
              <dd>{user?.roles.length ? user.roles.join(", ") : "Customer"}</dd>
            </div>
          </dl>
        </article>
        <article className="panel account-panel">
          <div>
            <p className="eyebrow">Session</p>
            <h3>Signed in</h3>
          </div>
          <dl className="detail-list">
            <div>
              <dt>Access expires</dt>
              <dd>{session ? formatDate(session.accessTokenExpiresAtUtc) : "Unavailable"}</dd>
            </div>
            <div>
              <dt>Refresh expires</dt>
              <dd>{session ? formatDate(session.refreshTokenExpiresAtUtc) : "Unavailable"}</dd>
            </div>
          </dl>
          <button type="button" className="button-secondary" onClick={() => void handleLogout()}>
            Sign out
          </button>
        </article>
        <article className="panel account-panel">
          <div>
            <p className="eyebrow">Coming next</p>
            <h3>Delivery preferences</h3>
          </div>
          <p className="muted">Saved addresses, default drop-off notes, and favorite cuisines can be added here without changing checkout again.</p>
        </article>
      </section>
    </section>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

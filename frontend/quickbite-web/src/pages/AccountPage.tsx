import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { ErrorState } from "../components/AsyncState";
import { createSavedAddress, deleteSavedAddress, getSavedAddresses } from "../services/quickbiteService";

export function AccountPage() {
  const { user, session, logout } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const addressesQuery = useQuery({
    queryKey: ["saved-addresses"],
    queryFn: getSavedAddresses
  });
  const createAddressMutation = useMutation({
    mutationFn: createSavedAddress,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["saved-addresses"] });
    }
  });
  const deleteAddressMutation = useMutation({
    mutationFn: deleteSavedAddress,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["saved-addresses"] });
    }
  });

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  function handleCreateAddress(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    createAddressMutation.mutate({
      label: String(formData.get("label") ?? ""),
      line1: String(formData.get("line1") ?? ""),
      line2: String(formData.get("line2") ?? "") || null,
      city: String(formData.get("city") ?? ""),
      state: String(formData.get("state") ?? ""),
      postalCode: String(formData.get("postalCode") ?? ""),
      country: String(formData.get("country") ?? ""),
      isDefault: formData.get("isDefault") === "on"
    });
    event.currentTarget.reset();
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
            <p className="eyebrow">Help</p>
            <h3>Need order support?</h3>
          </div>
          <p className="muted">If payment, delivery, or account status feels unclear, the support page gives calm next steps.</p>
          <button type="button" className="button-secondary" onClick={() => navigate("/support")}>
            Open support
          </button>
        </article>
      </section>
      <section className="panel account-panel" aria-label="Saved delivery addresses">
        <div className="section-heading section-heading--row">
          <div>
            <p className="eyebrow">Delivery</p>
            <h3>Saved addresses</h3>
          </div>
        </div>
        {addressesQuery.isLoading && <p className="muted">Loading saved addresses...</p>}
        {addressesQuery.isError && <ErrorState error={addressesQuery.error} />}
        {addressesQuery.isSuccess && addressesQuery.data.length === 0 && (
          <p className="muted">No saved addresses yet. Add one here or save one during checkout.</p>
        )}
        {addressesQuery.isSuccess && addressesQuery.data.length > 0 && (
          <div className="address-list">
            {addressesQuery.data.map((address) => (
              <article key={address.id} className="address-card">
                <div>
                  <strong>{address.label}{address.isDefault ? " (default)" : ""}</strong>
                  <p>{address.line1}</p>
                  <p className="muted">{address.city}, {address.state} {address.postalCode}</p>
                </div>
                <button
                  type="button"
                  className="button-secondary"
                  disabled={deleteAddressMutation.isPending}
                  onClick={() => deleteAddressMutation.mutate(address.id)}
                >
                  Remove
                </button>
              </article>
            ))}
          </div>
        )}
        <form className="checkout-form" onSubmit={handleCreateAddress}>
          <div className="checkout-address-grid">
            <label>
              Label
              <input name="label" defaultValue="Home" required maxLength={80} />
            </label>
            <label>
              Street address
              <input name="line1" defaultValue="123 Market Street" required maxLength={200} />
            </label>
          </div>
          <label>
            Apt, suite, or notes
            <input name="line2" maxLength={200} />
          </label>
          <div className="checkout-address-grid">
            <label>
              City
              <input name="city" defaultValue="Seattle" required maxLength={120} />
            </label>
            <label>
              State
              <input name="state" defaultValue="WA" required maxLength={80} />
            </label>
          </div>
          <div className="checkout-address-grid">
            <label>
              ZIP
              <input name="postalCode" defaultValue="98101" required maxLength={20} />
            </label>
            <label>
              Country
              <input name="country" defaultValue="USA" required maxLength={80} />
            </label>
          </div>
          <label className="checkbox-label">
            <input name="isDefault" type="checkbox" />
            Make this my default delivery address
          </label>
          <button type="submit" disabled={createAddressMutation.isPending}>
            {createAddressMutation.isPending ? "Saving..." : "Save address"}
          </button>
          {createAddressMutation.isError && <ErrorState error={createAddressMutation.error} />}
        </form>
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

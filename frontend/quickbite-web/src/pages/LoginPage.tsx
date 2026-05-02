import { FormEvent, useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { ErrorState } from "../components/AsyncState";
import { ApiError } from "../lib/apiErrors";

export function LoginPage() {
  const { isAuthenticated, login, authError } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<ApiError | null>(null);
  const redirectTo = typeof location.state === "object" && location.state !== null && "from" in location.state
    ? String(location.state.from)
    : "/orders";

  if (isAuthenticated) {
    return <Navigate to={redirectTo} replace />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    try {
      setIsSubmitting(true);
      setSubmitError(null);
      await login(email, password);
      navigate(redirectTo, { replace: true });
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error : authError);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="panel">
      <p className="eyebrow">Identity</p>
      <h2>Sign in to QuickBite</h2>
      <p className="muted">Use the seeded demo account or any user registered through the Identity API.</p>
      <form className="stack" onSubmit={handleSubmit}>
        <label>
          Email
          <input name="email" type="email" autoComplete="email" defaultValue="demo@quickbite.local" required />
        </label>
        <label>
          Password
          <input name="password" type="password" autoComplete="current-password" defaultValue="Pass123!" required />
        </label>
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>
      </form>
      {submitError && <ErrorState error={submitError} />}
    </section>
  );
}

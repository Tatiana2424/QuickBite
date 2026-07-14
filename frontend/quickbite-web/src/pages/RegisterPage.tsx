import { FormEvent, useState } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { ErrorState } from "../components/AsyncState";
import { ApiError } from "../lib/apiErrors";

export function RegisterPage() {
  const { isAuthenticated, register, authError } = useAuth();
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
    const fullName = String(formData.get("fullName") ?? "");
    const password = String(formData.get("password") ?? "");

    try {
      setIsSubmitting(true);
      setSubmitError(null);
      await register(email, fullName, password);
      navigate(redirectTo, { replace: true });
    } catch (error) {
      setSubmitError(error instanceof ApiError ? error : authError);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="panel">
      <p className="eyebrow">New here?</p>
      <h2>Create your QuickBite account</h2>
      <p className="muted">Save your session, place orders, and track payment and delivery updates.</p>
      <form className="stack" onSubmit={handleSubmit}>
        <label>
          Full name
          <input name="fullName" type="text" autoComplete="name" required maxLength={200} />
        </label>
        <label>
          Email
          <input name="email" type="email" autoComplete="email" required />
        </label>
        <label>
          Password
          <input name="password" type="password" autoComplete="new-password" required minLength={6} />
        </label>
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Creating account..." : "Create account"}
        </button>
      </form>
      <p className="muted">
        Already have an account? <Link className="text-link" to="/login">Sign in</Link>
      </p>
      {submitError && <ErrorState error={submitError} />}
    </section>
  );
}

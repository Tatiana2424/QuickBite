import type { ReactNode } from "react";
import { ApiError, toApiError } from "../lib/apiErrors";

export function LoadingState({ label }: { label: string }) {
  return (
    <div className="state-card" role="status" aria-live="polite">
      <span className="loader" aria-hidden="true" />
      <p>{label}</p>
    </div>
  );
}

export function EmptyState({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="state-card">
      <strong>{title}</strong>
      <p className="muted">{children}</p>
    </div>
  );
}

export function ErrorState({ error, action }: { error: unknown; action?: ReactNode }) {
  const apiError = error instanceof ApiError ? error : toApiError(error);

  return (
    <div className="state-card state-card--error" role="alert">
      <strong>{apiError.message}</strong>
      {apiError.traceId && <p className="muted">Trace id: {apiError.traceId}</p>}
      {action}
    </div>
  );
}

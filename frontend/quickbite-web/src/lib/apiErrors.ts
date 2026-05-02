import axios from "axios";

export type ApiErrorKind = "validation" | "unauthorized" | "notFound" | "transient" | "server" | "unknown";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly kind: ApiErrorKind,
    public readonly status?: number,
    public readonly traceId?: string
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (!axios.isAxiosError(error)) {
    return new ApiError("Something unexpected happened. Please try again.", "unknown");
  }

  if (!error.response) {
    return new ApiError("The gateway is unreachable. Check your connection and try again.", "transient");
  }

  const status = error.response.status;
  const data = error.response.data as ProblemDetails | undefined;
  const message = data?.detail || data?.title || defaultMessage(status);
  const traceId = data?.traceId;

  if (status === 400 || status === 422) {
    return new ApiError(message, "validation", status, traceId);
  }

  if (status === 401 || status === 403) {
    return new ApiError("Your session has expired. Please sign in again.", "unauthorized", status, traceId);
  }

  if (status === 404) {
    return new ApiError(message, "notFound", status, traceId);
  }

  if (status === 408 || status === 429 || status >= 500) {
    return new ApiError(message, status >= 500 ? "server" : "transient", status, traceId);
  }

  return new ApiError(message, "unknown", status, traceId);
}

function defaultMessage(status: number): string {
  if (status >= 500) {
    return "A QuickBite service is temporarily unavailable. Please try again.";
  }

  return "The request could not be completed.";
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  traceId?: string;
}

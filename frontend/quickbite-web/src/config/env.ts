const defaultApiBaseUrl = "http://localhost:8080";

export function normalizeApiBaseUrl(value: string | undefined): string {
  const candidate = value?.trim() || defaultApiBaseUrl;

  try {
    const normalized = new URL(candidate);
    return normalized.toString().replace(/\/$/, "");
  } catch {
    throw new Error("VITE_API_BASE_URL must be a valid absolute URL.");
  }
}

export const apiBaseUrl = normalizeApiBaseUrl(import.meta.env.VITE_API_BASE_URL);

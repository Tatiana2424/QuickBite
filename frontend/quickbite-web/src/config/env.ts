const defaultApiBaseUrl = "http://localhost:8080";
const supportedEnvironments = ["local", "development", "staging", "production"] as const;

export type AppEnvironment = (typeof supportedEnvironments)[number];

export interface AppConfig {
  apiBaseUrl: string;
  appEnvironment: AppEnvironment;
  monitoringEnabled: boolean;
}

export function normalizeApiBaseUrl(value: string | undefined): string {
  const candidate = value?.trim() || defaultApiBaseUrl;

  try {
    const normalized = new URL(candidate);
    return normalized.toString().replace(/\/$/, "");
  } catch {
    throw new Error("VITE_API_BASE_URL must be a valid absolute URL.");
  }
}

export function normalizeAppEnvironment(value: string | undefined): AppEnvironment {
  const candidate = (value?.trim().toLowerCase() || "local") as AppEnvironment;

  if (!supportedEnvironments.includes(candidate)) {
    throw new Error(`VITE_APP_ENV must be one of: ${supportedEnvironments.join(", ")}.`);
  }

  return candidate;
}

export function normalizeBoolean(value: string | undefined, defaultValue: boolean): boolean {
  if (value === undefined || value.trim() === "") {
    return defaultValue;
  }

  const normalized = value.trim().toLowerCase();
  if (["true", "1", "yes"].includes(normalized)) {
    return true;
  }

  if (["false", "0", "no"].includes(normalized)) {
    return false;
  }

  throw new Error("Boolean environment values must be true or false.");
}

export function createAppConfig(env: ImportMetaEnv): AppConfig {
  const appEnvironment = normalizeAppEnvironment(env.VITE_APP_ENV);

  return {
    apiBaseUrl: normalizeApiBaseUrl(env.VITE_API_BASE_URL),
    appEnvironment,
    monitoringEnabled: normalizeBoolean(env.VITE_ENABLE_MONITORING, appEnvironment === "production")
  };
}

export const appConfig = createAppConfig(import.meta.env);
export const apiBaseUrl = appConfig.apiBaseUrl;

import { describe, expect, it } from "vitest";
import { createAppConfig, normalizeApiBaseUrl, normalizeAppEnvironment, normalizeBoolean } from "../src/config/env";

describe("normalizeApiBaseUrl", () => {
  it("falls back to the local gateway when the value is missing", () => {
    expect(normalizeApiBaseUrl(undefined)).toBe("http://localhost:8080");
  });

  it("removes a trailing slash from a valid URL", () => {
    expect(normalizeApiBaseUrl("https://quickbite.local/")).toBe("https://quickbite.local");
  });

  it("throws when the value is not an absolute URL", () => {
    expect(() => normalizeApiBaseUrl("quickbite")).toThrow("VITE_API_BASE_URL must be a valid absolute URL.");
  });
});

describe("frontend environment validation", () => {
  it("accepts supported app environments", () => {
    expect(normalizeAppEnvironment("production")).toBe("production");
  });

  it("rejects unsupported app environments", () => {
    expect(() => normalizeAppEnvironment("prod")).toThrow("VITE_APP_ENV must be one of");
  });

  it("normalizes boolean feature flags", () => {
    expect(normalizeBoolean("true", false)).toBe(true);
    expect(normalizeBoolean("0", true)).toBe(false);
  });

  it("enables monitoring by default only in production", () => {
    expect(createAppConfig({ VITE_APP_ENV: "production" } as ImportMetaEnv).monitoringEnabled).toBe(true);
    expect(createAppConfig({ VITE_APP_ENV: "local" } as ImportMetaEnv).monitoringEnabled).toBe(false);
  });
});

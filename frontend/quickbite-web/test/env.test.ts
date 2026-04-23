import { describe, expect, it } from "vitest";
import { normalizeApiBaseUrl } from "../src/config/env";

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

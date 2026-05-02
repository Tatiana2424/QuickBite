import { describe, expect, it } from "vitest";
import {
  clearStoredSession,
  loadStoredSession,
  saveStoredSession,
  shouldRefreshAccessToken,
  toAuthSession
} from "../src/auth/authStorage";
import type { AuthResponse } from "../src/models";

describe("authStorage", () => {
  it("maps identity auth responses into frontend sessions", () => {
    const session = toAuthSession(authResponse());

    expect(session.user.email).toBe("demo@quickbite.local");
    expect(session.user.roles).toEqual(["Customer"]);
    expect(session.accessToken).toBe("access-token");
  });

  it("stores, loads, and clears a valid session", () => {
    const storage = new MemoryStorage();
    const session = toAuthSession(authResponse());

    saveStoredSession(session, storage);
    expect(loadStoredSession(storage)?.user.fullName).toBe("Demo Customer");

    clearStoredSession(storage);
    expect(loadStoredSession(storage)).toBeNull();
  });

  it("drops expired refresh-token sessions", () => {
    const storage = new MemoryStorage();
    const session = toAuthSession(
      authResponse({
        refreshTokenExpiresAtUtc: "2020-01-01T00:00:00Z"
      })
    );

    saveStoredSession(session, storage);

    expect(loadStoredSession(storage)).toBeNull();
  });

  it("refreshes access tokens before expiry", () => {
    const session = toAuthSession(
      authResponse({
        accessTokenExpiresAtUtc: "2026-05-02T10:00:30Z"
      })
    );

    expect(shouldRefreshAccessToken(session, Date.parse("2026-05-02T10:00:00Z"))).toBe(true);
  });
});

function authResponse(overrides: Partial<AuthResponse> = {}): AuthResponse {
  return {
    userId: "user-1",
    email: "demo@quickbite.local",
    fullName: "Demo Customer",
    roles: ["Customer"],
    accessToken: "access-token",
    accessTokenExpiresAtUtc: "2099-05-02T00:00:00Z",
    refreshToken: "refresh-token",
    refreshTokenExpiresAtUtc: "2099-05-09T00:00:00Z",
    ...overrides
  };
}

class MemoryStorage implements Storage {
  private readonly data = new Map<string, string>();

  get length() {
    return this.data.size;
  }

  clear(): void {
    this.data.clear();
  }

  getItem(key: string): string | null {
    return this.data.get(key) ?? null;
  }

  key(index: number): string | null {
    return Array.from(this.data.keys())[index] ?? null;
  }

  removeItem(key: string): void {
    this.data.delete(key);
  }

  setItem(key: string, value: string): void {
    this.data.set(key, value);
  }
}

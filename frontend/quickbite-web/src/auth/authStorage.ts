import type { AuthResponse, AuthSession } from "../models";

const storageKey = "quickbite.auth.session";
const refreshLeewayMilliseconds = 60_000;

export function toAuthSession(response: AuthResponse): AuthSession {
  return {
    user: {
      id: response.userId,
      email: response.email,
      fullName: response.fullName,
      roles: response.roles
    },
    accessToken: response.accessToken,
    accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
    refreshToken: response.refreshToken,
    refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc
  };
}

export function loadStoredSession(storage: Storage = window.localStorage): AuthSession | null {
  const value = storage.getItem(storageKey);

  if (!value) {
    return null;
  }

  try {
    const session = JSON.parse(value) as AuthSession;
    return isRefreshTokenExpired(session) ? null : session;
  } catch {
    storage.removeItem(storageKey);
    return null;
  }
}

export function saveStoredSession(session: AuthSession, storage: Storage = window.localStorage) {
  storage.setItem(storageKey, JSON.stringify(session));
}

export function clearStoredSession(storage: Storage = window.localStorage) {
  storage.removeItem(storageKey);
}

export function shouldRefreshAccessToken(session: AuthSession, now = Date.now()): boolean {
  return Date.parse(session.accessTokenExpiresAtUtc) - now <= refreshLeewayMilliseconds;
}

export function isRefreshTokenExpired(session: AuthSession, now = Date.now()): boolean {
  return Date.parse(session.refreshTokenExpiresAtUtc) <= now;
}

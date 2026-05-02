import { createContext, ReactNode, useContext, useEffect, useRef, useState } from "react";
import { configureApiAuth } from "../lib/api";
import { ApiError, toApiError } from "../lib/apiErrors";
import type { AuthSession, AuthUser } from "../models";
import { login as loginRequest, logout as logoutRequest, refreshSession as refreshSessionRequest } from "../services/quickbiteService";
import {
  clearStoredSession,
  isRefreshTokenExpired,
  loadStoredSession,
  saveStoredSession,
  shouldRefreshAccessToken,
  toAuthSession
} from "./authStorage";

interface AuthContextValue {
  user: AuthUser | null;
  session: AuthSession | null;
  isAuthenticated: boolean;
  authError: ApiError | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => loadStoredSession());
  const [authError, setAuthError] = useState<ApiError | null>(null);
  const sessionRef = useRef<AuthSession | null>(session);
  const refreshPromiseRef = useRef<Promise<string | null> | null>(null);

  useEffect(() => {
    sessionRef.current = session;
  }, [session]);

  useEffect(() => {
    configureApiAuth({
      getAccessToken,
      onUnauthorized: clearSession
    });
  }, []);

  async function login(email: string, password: string) {
    setAuthError(null);

    try {
      const response = await loginRequest(email, password);
      persistSession(toAuthSession(response));
    } catch (error) {
      const apiError = toApiError(error);
      setAuthError(apiError);
      throw apiError;
    }
  }

  async function logout() {
    const refreshToken = sessionRef.current?.refreshToken;
    clearSession();

    if (refreshToken) {
      await logoutRequest(refreshToken).catch(() => undefined);
    }
  }

  async function getAccessToken(): Promise<string | null> {
    const currentSession = sessionRef.current;

    if (!currentSession) {
      return null;
    }

    if (isRefreshTokenExpired(currentSession)) {
      clearSession();
      return null;
    }

    if (!shouldRefreshAccessToken(currentSession)) {
      return currentSession.accessToken;
    }

    refreshPromiseRef.current ??= refreshAccessToken(currentSession);

    try {
      return await refreshPromiseRef.current;
    } finally {
      refreshPromiseRef.current = null;
    }
  }

  async function refreshAccessToken(currentSession: AuthSession): Promise<string | null> {
    try {
      const response = await refreshSessionRequest(currentSession.refreshToken);
      const refreshedSession = toAuthSession(response);
      persistSession(refreshedSession);
      return refreshedSession.accessToken;
    } catch {
      clearSession();
      return null;
    }
  }

  function persistSession(nextSession: AuthSession) {
    sessionRef.current = nextSession;
    saveStoredSession(nextSession);
    setSession(nextSession);
  }

  function clearSession() {
    sessionRef.current = null;
    clearStoredSession();
    setSession(null);
  }

  return (
    <AuthContext.Provider
      value={{
        user: session?.user ?? null,
        session,
        isAuthenticated: Boolean(session),
        authError,
        login,
        logout
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const value = useContext(AuthContext);

  if (!value) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return value;
}

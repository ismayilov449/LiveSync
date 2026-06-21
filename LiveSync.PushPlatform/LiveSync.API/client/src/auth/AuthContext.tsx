import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { authApi } from '../api';
import type { AuthSession, LoginRequest, RegisterRequest } from '../types';

const STORAGE_KEY = 'livesync.auth';
const TENANT_ADMIN = 'TenantAdmin';

interface AuthContextValue {
  session: AuthSession | null;
  roles: string[];
  isAuthenticated: boolean;
  isTenantAdmin: boolean;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function loadSession(): AuthSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as AuthSession;
    if (new Date(session.expiresAtUtc) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

function saveSession(session: AuthSession | null) {
  if (session) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } else {
    localStorage.removeItem(STORAGE_KEY);
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => loadSession());
  const [roles, setRoles] = useState<string[]>([]);

  const applySession = useCallback((next: AuthSession) => {
    saveSession(next);
    setSession(next);
  }, []);

  const login = useCallback(async (request: LoginRequest) => {
    const result = await authApi.login(request);
    applySession(result);
  }, [applySession]);

  const register = useCallback(async (request: RegisterRequest) => {
    const result = await authApi.register(request);
    applySession(result);
  }, [applySession]);

  const logout = useCallback(() => {
    saveSession(null);
    setSession(null);
    setRoles([]);
  }, []);

  useEffect(() => {
    if (!session) {
      setRoles([]);
      return;
    }

    let cancelled = false;
    void authApi.me(session.accessToken)
      .then((profile) => {
        if (!cancelled) setRoles(profile.roles);
      })
      .catch(() => {
        if (!cancelled) setRoles([]);
      });

    return () => { cancelled = true; };
  }, [session]);

  const value = useMemo<AuthContextValue>(() => ({
    session,
    roles,
    isAuthenticated: session != null,
    isTenantAdmin: roles.includes(TENANT_ADMIN),
    login,
    register,
    logout,
  }), [session, roles, login, register, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

export function useAccessToken(): string | null {
  return useAuth().session?.accessToken ?? null;
}

export function useIsTenantAdmin(): boolean {
  return useAuth().isTenantAdmin;
}

import { useCallback, useEffect, useState } from 'react';
import { authApi } from '../api';
import { useAccessToken } from '../auth/AuthContext';
import type { UserProfile } from '../types';

export function useProfile() {
  const token = useAccessToken();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const reload = useCallback(async () => {
    if (!token) {
      setProfile(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await authApi.me(token);
      setProfile(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load profile.');
      setProfile(null);
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { profile, error, loading, reload };
}

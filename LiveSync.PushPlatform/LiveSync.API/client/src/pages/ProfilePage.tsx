import { useEffect, useState } from 'react';
import { authApi } from '../api';
import { useAuth } from '../auth/AuthContext';
import { InviteUserPage } from './InviteUserPage';
import type { UserProfile } from '../types';

export function ProfilePage() {
  const { session } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!session) return;

    let cancelled = false;
    (async () => {
      try {
        const data = await authApi.me(session.accessToken);
        if (!cancelled) setProfile(data);
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load profile.');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [session]);

  if (loading) {
    return <div className="panel"><p className="muted">Loading profile…</p></div>;
  }

  if (error || !profile || !session) {
    return <div className="panel"><div className="alert alert-error">{error ?? 'Profile unavailable.'}</div></div>;
  }

  return (
    <>
      <div className="panel">
        <div className="panel-header">
          <h2>Profile</h2>
          <p className="muted">Your account in the LiveSync control plane</p>
        </div>
        <dl className="profile-grid">
          <div>
            <dt>Display name</dt>
            <dd>{profile.displayName || '—'}</dd>
          </div>
          <div>
            <dt>Username</dt>
            <dd>{profile.userName}</dd>
          </div>
          <div>
            <dt>Email</dt>
            <dd>{profile.email || '—'}</dd>
          </div>
          <div>
            <dt>User ID</dt>
            <dd>{profile.userId}</dd>
          </div>
          <div>
            <dt>Tenant ID</dt>
            <dd>{profile.tenantId}</dd>
          </div>
          <div>
            <dt>Roles</dt>
            <dd>{profile.roles.length > 0 ? profile.roles.join(', ') : '—'}</dd>
          </div>
          <div>
            <dt>Session expires (UTC)</dt>
            <dd>{new Date(session.expiresAtUtc).toLocaleString()}</dd>
          </div>
        </dl>
      </div>
      <InviteUserPage profile={profile} id="invite" />
    </>
  );
}

import { useAuth } from '../auth/AuthContext';
import { useProfile } from '../hooks/useProfile';
import { TenantStatusChip } from '../components/TenantStatusChip';

export function ProfilePage() {
  const { session } = useAuth();
  const { profile, error, loading } = useProfile();

  if (loading) {
    return <div className="panel"><p className="muted">Loading profile…</p></div>;
  }

  if (error || !profile || !session) {
    return <div className="panel"><div className="alert alert-error">{error ?? 'Profile unavailable.'}</div></div>;
  }

  return (
    <div className="panel">
      <div className="panel-header">
        <h2>Account</h2>
        <p className="muted">Your user profile in the LiveSync control plane</p>
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
          <dd className="mono">{profile.userId}</dd>
        </div>
        <div>
          <dt>Organization</dt>
          <dd>{profile.tenantName || '—'}</dd>
        </div>
        <div>
          <dt>Tenant ID</dt>
          <dd className="mono tabular">{profile.tenantId}</dd>
        </div>
        <div>
          <dt>Tenant status</dt>
          <dd><TenantStatusChip status={profile.tenantStatus} /></dd>
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
  );
}

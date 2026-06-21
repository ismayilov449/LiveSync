import { useState, type FormEvent } from 'react';
import { authApi } from '../api';
import { useAccessToken } from '../auth/AuthContext';
import { HttpError } from '../api/http';
import type { UserProfile } from '../types';

interface InviteUserPageProps {
  profile: UserProfile | null;
  id?: string;
}

export function InviteUserPage({ profile, id }: InviteUserPageProps) {
  const token = useAccessToken();
  const [userName, setUserName] = useState('');
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const isAdmin = profile?.roles.includes('TenantAdmin') ?? false;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!token) return;
    setError(null);
    setSuccess(null);
    setLoading(true);
    try {
      const created = await authApi.createUser(token, {
        userName: userName.trim(),
        email: email.trim(),
        displayName: displayName.trim(),
        password,
      });
      setSuccess(`Created user ${created.userName} (ID ${created.userId}) in tenant ${created.tenantId}.`);
      setUserName('');
      setEmail('');
      setDisplayName('');
      setPassword('');
    } catch (err) {
      setError(err instanceof HttpError ? err.message : 'Invite failed.');
    } finally {
      setLoading(false);
    }
  };

  if (!isAdmin) {
    return (
      <div className="panel">
        <div className="panel-header">
          <h2>Invite user</h2>
          <p className="muted">Only tenant administrators can invite users.</p>
        </div>
        <div className="alert alert-error">
          Your account does not have the TenantAdmin role.
        </div>
      </div>
    );
  }

  return (
    <div className="panel" id={id}>
      <div className="panel-header">
        <h2>Invite user</h2>
        <p className="muted">
          Add a user to tenant {profile?.tenantId}. New users join your tenant automatically.
        </p>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <form className="stack-form" onSubmit={handleSubmit}>
        <label className="field">
          <span>Display name</span>
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
        </label>
        <label className="field">
          <span>Username</span>
          <input value={userName} onChange={(e) => setUserName(e.target.value)} required />
        </label>
        <label className="field">
          <span>Email</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label className="field">
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            minLength={8}
            required
          />
        </label>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? 'Creating…' : 'Create user'}
        </button>
      </form>
    </div>
  );
}

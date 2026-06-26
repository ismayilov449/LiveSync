import { useState, type FormEvent } from 'react';
import { authApi } from '../../api';
import { useAccessToken } from '../../auth/AuthContext';
import { HttpError } from '../../api/http';
import { useProfile } from '../../hooks/useProfile';

export function AdminUsersPage() {
  const token = useAccessToken();
  const { profile } = useProfile();
  const [userName, setUserName] = useState('');
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

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
      setSuccess(`Created user ${created.userName} (ID ${created.userId}).`);
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

  return (
    <div className="admin-page">
      <div className="panel">
        <div className="panel-header">
          <h2>Users</h2>
          <p className="muted">
            Invite users to {profile?.tenantName || `tenant ${profile?.tenantId}`}. New accounts
            receive the TenantUser role.
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
    </div>
  );
}

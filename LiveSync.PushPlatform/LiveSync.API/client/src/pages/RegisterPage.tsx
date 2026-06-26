import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { AuthLayout, AuthLink } from '../components/AuthLayout';
import { HttpError } from '../api/http';

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [tenantName, setTenantName] = useState('');
  const [userName, setUserName] = useState('');
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await register({ tenantName, userName, email, password, displayName });
      navigate('/tickets', { replace: true });
    } catch (err) {
      setError(err instanceof HttpError
        ? err.message
        : err instanceof Error ? err.message : 'Registration failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout
      title="Create account"
      subtitle="Creates a new organization (tenant) and your admin account"
      footer={<>Already registered? <AuthLink to="/login">Sign in</AuthLink></>}
      onSubmit={handleSubmit}
    >
      {error && <div className="alert alert-error">{error}</div>}
      <label className="field">
        <span>Organization / tenant name</span>
        <input
          value={tenantName}
          onChange={(e) => setTenantName(e.target.value)}
          placeholder="Acme Corp"
          required
        />
      </label>
      <label className="field">
        <span>Display name</span>
        <input
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          placeholder="Jane Admin"
          required
        />
      </label>
      <label className="field">
        <span>Username</span>
        <input
          value={userName}
          onChange={(e) => setUserName(e.target.value)}
          autoComplete="username"
          required
        />
      </label>
      <label className="field">
        <span>Email</span>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
          required
        />
      </label>
      <label className="field">
        <span>Password</span>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="new-password"
          minLength={8}
          required
        />
      </label>
      <button type="submit" className="btn btn-primary" disabled={loading}>
        {loading ? 'Creating…' : 'Create account'}
      </button>
    </AuthLayout>
  );
}

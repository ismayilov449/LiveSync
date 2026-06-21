import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { AuthLayout, AuthLink } from '../components/AuthLayout';
import { HttpError } from '../api/http';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login({ userName, password });
      navigate('/items', { replace: true });
    } catch (err) {
      setError(err instanceof HttpError && err.status === 401
        ? 'Invalid username or password.'
        : err instanceof Error ? err.message : 'Login failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout
      title="Sign in"
      subtitle="Access your tenant workspace"
      footer={<>No account? <AuthLink to="/register">Create one</AuthLink></>}
      onSubmit={handleSubmit}
    >
      {error && <div className="alert alert-error">{error}</div>}
      <p className="muted form-hint">Dev seed: admin@livesync.local / Admin123!</p>
      <label className="field">
        <span>Username or email</span>
        <input
          value={userName}
          onChange={(e) => setUserName(e.target.value)}
          autoComplete="username"
          required
        />
      </label>
      <label className="field">
        <span>Password</span>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
        />
      </label>
      <button type="submit" className="btn btn-primary" disabled={loading}>
        {loading ? 'Signing in…' : 'Sign in'}
      </button>
    </AuthLayout>
  );
}

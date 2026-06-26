import { useState } from 'react';
import { tenantApi } from '../../api';
import { useAccessToken } from '../../auth/AuthContext';
import { HttpError } from '../../api/http';
import { useProfile } from '../../hooks/useProfile';
import { ConfirmDialog } from '../../components/Modal';
import { TenantStatusChip } from '../../components/TenantStatusChip';

export function AdminSettingsPage() {
  const token = useAccessToken();
  const { profile, reload } = useProfile();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [confirmSuspend, setConfirmSuspend] = useState(false);

  const isSuspended = profile?.tenantStatus.toLowerCase() === 'suspended';
  const isActive = profile?.tenantStatus.toLowerCase() === 'active';

  const handleSuspend = async () => {
    if (!token) return;
    setError(null);
    setSuccess(null);
    setLoading(true);
    try {
      await tenantApi.suspend(token);
      setSuccess('Tenant suspended. Item access is blocked until you reactivate.');
      setConfirmSuspend(false);
      await reload();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : 'Suspend failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleReactivate = async () => {
    if (!token) return;
    setError(null);
    setSuccess(null);
    setLoading(true);
    try {
      await tenantApi.reactivate(token);
      setSuccess('Tenant reactivated. Users can access items again.');
      await reload();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : 'Reactivate failed.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="admin-page">
      <div className="panel">
        <div className="panel-header">
          <h2>Settings</h2>
          <p className="muted">Organization lifecycle controls</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {success && <div className="alert alert-success">{success}</div>}

        <dl className="profile-grid">
          <div>
            <dt>Organization</dt>
            <dd>{profile?.tenantName || '—'}</dd>
          </div>
          <div>
            <dt>Tenant ID</dt>
            <dd>{profile?.tenantId ?? '—'}</dd>
          </div>
          <div>
            <dt>Status</dt>
            <dd>
              {profile ? <TenantStatusChip status={profile.tenantStatus} /> : '—'}
            </dd>
          </div>
        </dl>

        <div className="admin-actions">
          {isActive && (
            <button
              type="button"
              className="btn btn-danger"
              disabled={loading}
              onClick={() => setConfirmSuspend(true)}
            >
              Suspend organization
            </button>
          )}
          {isSuspended && (
            <button
              type="button"
              className="btn btn-primary"
              disabled={loading}
              onClick={() => void handleReactivate()}
            >
              {loading ? 'Reactivating…' : 'Reactivate organization'}
            </button>
          )}
        </div>

        <p className="muted admin-hint-text">
          Suspending blocks all item API access for this tenant. Reactivate is always available to
          tenant administrators.
        </p>
      </div>

      <ConfirmDialog
        open={confirmSuspend}
        title="Suspend organization?"
        message={`Users in ${profile?.tenantName ?? 'this tenant'} will not be able to load or modify items until the organization is reactivated.`}
        confirmLabel={loading ? 'Suspending…' : 'Confirm suspend'}
        tone="danger"
        onConfirm={() => void handleSuspend()}
        onCancel={() => setConfirmSuspend(false)}
      />
    </div>
  );
}

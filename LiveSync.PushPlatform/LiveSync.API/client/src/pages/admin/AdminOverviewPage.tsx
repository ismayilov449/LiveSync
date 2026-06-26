import { useEffect, useState } from 'react';
import { operationsApi } from '../../api';
import { useAccessToken } from '../../auth/AuthContext';
import { useProfile } from '../../hooks/useProfile';
import { TenantStatusChip } from '../../components/TenantStatusChip';
import type { ChangeQueueStats } from '../../types';

export function AdminOverviewPage() {
  const token = useAccessToken();
  const { profile, loading: profileLoading } = useProfile();
  const [stats, setStats] = useState<ChangeQueueStats | null>(null);
  const [statsError, setStatsError] = useState<string | null>(null);
  const [statsLoading, setStatsLoading] = useState(true);

  useEffect(() => {
    if (!token) return;

    let cancelled = false;
    (async () => {
      setStatsLoading(true);
      setStatsError(null);
      try {
        const data = await operationsApi.changeQueue(token);
        if (!cancelled) setStats(data);
      } catch (err) {
        if (!cancelled) {
          setStatsError(err instanceof Error ? err.message : 'Failed to load queue stats.');
        }
      } finally {
        if (!cancelled) setStatsLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [token]);

  const isSuspended = profile?.tenantStatus.toLowerCase() === 'suspended';

  return (
    <div className="admin-page">
      <div className="panel">
        <div className="panel-header">
          <h2>Overview</h2>
          <p className="muted">Organization health and background processing</p>
        </div>

        {isSuspended && (
          <div className="alert alert-error">
            This tenant is suspended. Reactivate it under Settings to restore access to items.
          </div>
        )}

        <div className="stat-grid">
          <div className="stat-card">
            <span className="stat-label">Organization</span>
            <strong className="stat-value">
              {profileLoading ? '…' : profile?.tenantName || '—'}
            </strong>
            <span className="muted mono tabular">ID {profile?.tenantId ?? '—'}</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Status</span>
            <div className="stat-value">
              {profile ? <TenantStatusChip status={profile.tenantStatus} /> : '—'}
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Queue pending</span>
            <strong className="stat-value">
              {statsLoading ? '…' : stats?.pendingCount ?? '—'}
            </strong>
            <span className="muted">Change detection outbox</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Dead letter</span>
            <strong className="stat-value">
              {statsLoading ? '…' : stats?.deadLetterCount ?? '—'}
            </strong>
            <span className="muted">Failed after max retries</span>
          </div>
        </div>

        {statsError && <div className="alert alert-error">{statsError}</div>}

        <div className="admin-hint panel-nested">
          <h3>Operability</h3>
          <p className="muted">
            Queue metrics update when the Worker is running. Prometheus metrics are exposed at{' '}
            <code>/metrics</code> on the API and Worker.
          </p>
        </div>
      </div>
    </div>
  );
}

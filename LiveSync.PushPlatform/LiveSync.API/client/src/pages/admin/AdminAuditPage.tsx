import { useEffect, useState } from 'react';
import { auditApi } from '../../api';
import { useAccessToken } from '../../auth/AuthContext';
import type { AuditEvent } from '../../types';

const PAGE_SIZE = 20;

export function AdminAuditPage() {
  const token = useAccessToken();
  const [items, setItems] = useState<AuditEvent[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  useEffect(() => {
    if (!token) return;

    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await auditApi.list(token, page, PAGE_SIZE);
        if (!cancelled) {
          setItems(data.items);
          setTotalCount(data.totalCount);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load audit log.');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [token, page]);

  return (
    <div className="admin-page">
      <div className="panel">
        <div className="panel-header">
          <h2>Audit log</h2>
          <p className="muted">Administrative actions recorded for this tenant</p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        {loading ? (
          <p className="muted">Loading audit events…</p>
        ) : items.length === 0 ? (
          <p className="muted">No audit events yet. Create items or change tenant settings to generate entries.</p>
        ) : (
          <div className="table-scroll">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Time (UTC)</th>
                  <th>User</th>
                  <th>Action</th>
                  <th>Entity</th>
                  <th>Details</th>
                </tr>
              </thead>
              <tbody>
                {items.map((event) => (
                  <tr key={event.id}>
                    <td>{new Date(event.createdAtUtc).toLocaleString()}</td>
                    <td>{event.userId}</td>
                    <td><code>{event.action}</code></td>
                    <td>
                      <code>{event.entityType}</code>
                      {event.entityId ? ` #${event.entityId}` : ''}
                    </td>
                    <td>{event.details ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {totalCount > 0 && (
          <div className="pagination">
            <span className="muted">
              {totalCount} event{totalCount === 1 ? '' : 's'} · page {page} of {totalPages}
            </span>
            <div className="pagination-actions">
              <button
                type="button"
                className="btn btn-sm"
                disabled={page <= 1 || loading}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Previous
              </button>
              <button
                type="button"
                className="btn btn-sm"
                disabled={page >= totalPages || loading}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

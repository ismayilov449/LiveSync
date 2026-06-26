import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { queuesApi } from '../api';
import { useAccessToken, useAuth, useIsTenantAdmin } from '../auth/AuthContext';
import { ConfirmDialog, PromptDialog } from '../components/Modal';
import { RemotePushFlash } from '../components/RemotePushFlash';
import { useQueuesPush, type PushConnectionStatus } from '../hooks/useSupportDeskPush';
import { useRemotePushHighlights } from '../hooks/useRemotePushHighlights';
import type { ChangeNotificationDto, Queue } from '../types';
import {
  PushOperation,
  applyDeletePatch,
  applyUpsertPatch,
  parseEntityNumericId,
  sortQueuesNewestFirst,
} from '../utils/pushListPatch';

const PAGE_SIZE = 20;

type DialogState =
  | { type: 'rename'; queue: Queue }
  | { type: 'deactivate'; queue: Queue }
  | { type: 'delete'; queue: Queue }
  | null;

function formatCreatedAt(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function pushStatusTone(status: PushConnectionStatus): 'live' | 'warn' | 'off' {
  switch (status) {
    case 'connected':
      return 'live';
    case 'connecting':
    case 'reconnecting':
      return 'warn';
    default:
      return 'off';
  }
}

function pushStatusLabel(status: PushConnectionStatus) {
  switch (status) {
    case 'connected':
      return 'live';
    case 'connecting':
      return 'connecting…';
    case 'reconnecting':
      return 'reconnecting…';
    default:
      return 'offline';
  }
}

function PushStatus({ status }: { status: PushConnectionStatus }) {
  return (
    <span className="status-pill" data-tone={pushStatusTone(status)}>
      <span className="status-dot" aria-hidden />
      signalr · {pushStatusLabel(status)}
    </span>
  );
}

function QueueStatus({ active }: { active: boolean }) {
  return (
    <span className="status-pill" data-tone={active ? 'live' : 'off'}>
      <span className="status-dot" aria-hidden />
      {active ? 'active' : 'inactive'}
    </span>
  );
}

export function QueuesPage() {
  const token = useAccessToken();
  const { session } = useAuth();
  const isTenantAdmin = useIsTenantAdmin();
  const [queues, setQueues] = useState<Queue[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [tableSyncing, setTableSyncing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);
  const [createName, setCreateName] = useState('');
  const { markOwnAction, noteRemotePush, clearHighlights, isHighlighted } = useRemotePushHighlights();
  const loadSeqRef = useRef(0);
  const pageRef = useRef(page);
  const totalCountRef = useRef(totalCount);

  pageRef.current = page;
  totalCountRef.current = totalCount;

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const loadQueues = useCallback(async (targetPage = page, options?: { silent?: boolean }) => {
    if (!token) return;
    const seq = ++loadSeqRef.current;
    setError(null);
    if (options?.silent) setTableSyncing(true);
    else setLoading(true);

    try {
      const data = await queuesApi.list(token, { page: targetPage, pageSize: PAGE_SIZE });
      if (seq !== loadSeqRef.current) return;
      setQueues(data.items);
      setTotalCount(data.totalCount);
      totalCountRef.current = data.totalCount;
      setPage(data.page);
      pageRef.current = data.page;
    } catch (err) {
      if (seq !== loadSeqRef.current) return;
      setError(err instanceof Error ? err.message : 'Failed to load queues.');
    } finally {
      if (seq === loadSeqRef.current) {
        setLoading(false);
        setTableSyncing(false);
      }
    }
  }, [token, page]);

  useEffect(() => {
    void loadQueues(page);
  }, [page, token]); // eslint-disable-line react-hooks/exhaustive-deps

  const reloadTable = useCallback(async (targetPage = pageRef.current) => {
    if (!token) return;
    setTableSyncing(true);
    try {
      const data = await queuesApi.list(token, { page: targetPage, pageSize: PAGE_SIZE });
      setQueues(data.items);
      setTotalCount(data.totalCount);
      totalCountRef.current = data.totalCount;
    } catch {
      // ignore background table reload failures
    } finally {
      setTableSyncing(false);
    }
  }, [token]);

  const handlePushUpdate = useCallback(async (notification: ChangeNotificationDto) => {
    if (!token) return;

    const entityId = parseEntityNumericId(notification.entity.id);
    if (entityId == null) return;

    if (notification.operation === PushOperation.Delete) {
      setQueues((prev) => {
        const result = applyDeletePatch(prev, totalCountRef.current, entityId);
        if (result.patched) {
          totalCountRef.current = result.totalCount;
          setTotalCount(result.totalCount);
        }
        return result.items;
      });
      return;
    }

    setTableSyncing(true);
    try {
      const queue = await queuesApi.get(token, entityId);
      let needsFallback = false;

      setQueues((prev) => {
        const result = applyUpsertPatch(
          prev,
          totalCountRef.current,
          pageRef.current,
          PAGE_SIZE,
          queue,
          sortQueuesNewestFirst,
        );
        totalCountRef.current = result.totalCount;
        setTotalCount(result.totalCount);
        needsFallback = !result.patched && pageRef.current === 1;
        return result.patched ? result.items : prev;
      });

      if (needsFallback) {
        await reloadTable(pageRef.current);
      }

      noteRemotePush(entityId);
    } catch {
      await reloadTable(pageRef.current);
    } finally {
      setTableSyncing(false);
    }
  }, [token, reloadTable, noteRemotePush]);

  const pushStatus = useQueuesPush(token, session?.tenantId, handlePushUpdate);

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!token) return;
    setError(null);
    try {
      const newId = await queuesApi.create(token, { name: createName.trim() });
      markOwnAction(newId);
      setCreateName('');
      setPage(1);
      await loadQueues(1);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Create failed.');
    }
  };

  const runAction = async (queueId: number, action: () => Promise<void>, onSuccess?: () => void) => {
    markOwnAction(queueId);
    setBusyId(queueId);
    setError(null);
    try {
      await action();
      if (onSuccess) onSuccess();
      else await loadQueues(page, { silent: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed.');
    } finally {
      setBusyId(null);
    }
  };

  const handleRenameConfirm = async (nextName: string) => {
    if (!token || dialog?.type !== 'rename') return;
    const queue = dialog.queue;
    if (nextName === queue.name) {
      setDialog(null);
      return;
    }
    await runAction(queue.id, () => queuesApi.update(token, queue.id, { name: nextName }));
    setDialog(null);
  };

  const handleDeactivateConfirm = async () => {
    if (!token || dialog?.type !== 'deactivate') return;
    const queue = dialog.queue;
    await runAction(queue.id, () => queuesApi.deactivate(token, queue.id));
    setDialog(null);
  };

  const handleDeleteConfirm = async () => {
    if (!token || dialog?.type !== 'delete') return;
    const queue = dialog.queue;
    const nextPage = queues.length === 1 && page > 1 ? page - 1 : page;
    await runAction(
      queue.id,
      () => queuesApi.delete(token, queue.id),
      async () => {
        setPage(nextPage);
        await loadQueues(nextPage, { silent: true });
      },
    );
    setDialog(null);
  };

  return (
    <div className="queues-page">
      <div className="panel">
        <div className="panel-header row-between">
          <div>
            <h2>Queues</h2>
            <p className="muted row-gap">
              <span>newest first</span>
              <PushStatus status={pushStatus} />
            </p>
          </div>
          <button
            type="button"
            className="btn btn-ghost"
            disabled={tableSyncing}
            onClick={() => {
              clearHighlights();
              void loadQueues(page, { silent: true });
            }}
          >
            Refresh
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form className="inline-form" onSubmit={handleCreate}>
          <label className="field grow">
            <span>Name</span>
            <input
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
              placeholder="New queue name"
              required
            />
          </label>
          <button type="submit" className="btn btn-primary">
            Create
          </button>
        </form>
      </div>

      <div className={`panel panel-table${tableSyncing ? ' panel-table-syncing' : ''}`}>
        {tableSyncing && !loading ? (
          <p className="table-sync-hint muted mono">updating…</p>
        ) : null}

        {loading ? (
          <p className="muted">Loading queues…</p>
        ) : queues.length === 0 ? (
          <p className="muted">No queues yet.</p>
        ) : (
          <>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Name</th>
                  <th>Created</th>
                  <th>Status</th>
                  <th>Actions</th>
                  <th className="remote-col" aria-hidden />
                </tr>
              </thead>
              <tbody>
                {queues.map((queue) => (
                  <tr key={queue.id} className={!queue.isActive ? 'inactive' : ''}>
                    <td className="mono tabular">{queue.id}</td>
                    <td>{queue.name}</td>
                    <td>{formatCreatedAt(queue.createdAtUtc)}</td>
                    <td>
                      <QueueStatus active={queue.isActive} />
                    </td>
                    <td className="actions">
                      <button
                        type="button"
                        className="btn btn-sm"
                        disabled={busyId === queue.id}
                        onClick={() => setDialog({ type: 'rename', queue })}
                      >
                        Rename
                      </button>
                      {isTenantAdmin && (
                        <>
                          <button
                            type="button"
                            className="btn btn-sm"
                            disabled={busyId === queue.id || !queue.isActive}
                            onClick={() => setDialog({ type: 'deactivate', queue })}
                          >
                            Deactivate
                          </button>
                          <button
                            type="button"
                            className="btn btn-sm btn-danger"
                            disabled={busyId === queue.id}
                            onClick={() => setDialog({ type: 'delete', queue })}
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </td>
                    <td className="remote-col">
                      <RemotePushFlash show={isHighlighted(queue.id)} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="pagination">
              <span className="muted mono tabular">
                {totalCount} queue{totalCount === 1 ? '' : 's'} · page {page} of {totalPages}
              </span>
              <div className="pagination-actions">
                <button
                  type="button"
                  className="btn btn-sm"
                  disabled={page <= 1 || tableSyncing}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </button>
                <button
                  type="button"
                  className="btn btn-sm"
                  disabled={page >= totalPages || tableSyncing}
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}
      </div>

      <PromptDialog
        open={dialog?.type === 'rename'}
        title="Rename queue"
        label="New name"
        initialValue={dialog?.type === 'rename' ? dialog.queue.name : ''}
        onConfirm={(value) => void handleRenameConfirm(value)}
        onCancel={() => setDialog(null)}
      />

      <ConfirmDialog
        open={dialog?.type === 'deactivate'}
        title="Deactivate queue"
        message={dialog?.type === 'deactivate' ? `Deactivate "${dialog.queue.name}"?` : ''}
        confirmLabel="Deactivate"
        onConfirm={() => void handleDeactivateConfirm()}
        onCancel={() => setDialog(null)}
      />

      <ConfirmDialog
        open={dialog?.type === 'delete'}
        title="Delete queue"
        message={dialog?.type === 'delete' ? `Delete "${dialog.queue.name}" permanently?` : ''}
        confirmLabel="Delete"
        tone="danger"
        onConfirm={() => void handleDeleteConfirm()}
        onCancel={() => setDialog(null)}
      />
    </div>
  );
}

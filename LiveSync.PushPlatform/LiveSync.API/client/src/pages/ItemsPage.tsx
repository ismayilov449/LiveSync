import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { itemsApi } from '../api';
import { useAccessToken, useAuth, useIsTenantAdmin } from '../auth/AuthContext';
import { ConfirmDialog, PromptDialog } from '../components/Modal';
import { useItemsPush, type PushConnectionStatus } from '../hooks/useItemsPush';
import type { Item } from '../types';

const PAGE_SIZE = 20;
const PARENT_OPTIONS_PAGE_SIZE = 100;

type DialogState =
  | { type: 'rename'; item: Item }
  | { type: 'move'; item: Item }
  | { type: 'deactivate'; item: Item }
  | { type: 'delete'; item: Item }
  | null;

function formatCreatedAt(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function pickDefaultParent(options: Item[]) {
  if (options.length === 0) return '';
  const root = options.find((item) => item.name === 'Root');
  const fallback = options.reduce((min, item) => (item.id < min.id ? item : min));
  return String((root ?? fallback).id);
}

function formatParentOption(item: Item) {
  return `#${item.id} · ${item.name}`;
}

function pushStatusLabel(status: PushConnectionStatus) {
  switch (status) {
    case 'connected':
      return 'Live';
    case 'connecting':
      return 'Connecting…';
    case 'reconnecting':
      return 'Reconnecting…';
    default:
      return 'Offline';
  }
}

function pushStatusClass(status: PushConnectionStatus) {
  switch (status) {
    case 'connected':
      return 'badge-success';
    case 'connecting':
    case 'reconnecting':
      return 'badge-warning';
    default:
      return 'badge-muted';
  }
}

export function ItemsPage() {
  const token = useAccessToken();
  const { session } = useAuth();
  const isTenantAdmin = useIsTenantAdmin();
  const [items, setItems] = useState<Item[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);

  const [createParentId, setCreateParentId] = useState('');
  const [createName, setCreateName] = useState('');
  const [parentOptions, setParentOptions] = useState<Item[]>([]);
  const [loadingParents, setLoadingParents] = useState(true);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const loadParentOptions = useCallback(async () => {
    if (!token) return;
    setLoadingParents(true);
    try {
      const data = await itemsApi.list(token, { page: 1, pageSize: PARENT_OPTIONS_PAGE_SIZE });
      const options = [...data.items].sort((a, b) => a.id - b.id);
      setParentOptions(options);
      setCreateParentId((current) => {
        if (current && options.some((item) => item.id === Number(current))) {
          return current;
        }
        return pickDefaultParent(options);
      });
    } catch {
      setParentOptions([]);
    } finally {
      setLoadingParents(false);
    }
  }, [token]);

  const loadItems = useCallback(async (targetPage = page, options?: { silent?: boolean }) => {
    if (!token) return;
    setError(null);
    if (options?.silent) {
      setRefreshing(true);
    } else {
      setLoading(true);
    }
    try {
      const data = await itemsApi.list(token, { page: targetPage, pageSize: PAGE_SIZE });
      setItems(data.items);
      setTotalCount(data.totalCount);
      setPage(data.page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load items.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [token, page]);

  useEffect(() => {
    void loadItems(page);
  }, [page, token]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    void loadParentOptions();
  }, [loadParentOptions]);

  const refreshCurrentPage = useCallback(() => {
    void loadItems(page, { silent: true });
    void loadParentOptions();
  }, [loadItems, loadParentOptions, page]);

  const pushStatus = useItemsPush(token, session?.tenantId, refreshCurrentPage);

  const handleCreate = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!token || !createParentId) return;
    setError(null);
    try {
      await itemsApi.create(token, {
        parentId: Number(createParentId),
        name: createName.trim(),
      });
      setCreateName('');
      setPage(1);
      await Promise.all([loadItems(1), loadParentOptions()]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Create failed.');
    }
  };

  const runItemAction = async (itemId: number, action: () => Promise<void>, onSuccess?: () => void) => {
    setBusyId(itemId);
    setError(null);
    try {
      await action();
      if (onSuccess) onSuccess();
      else await loadItems(page, { silent: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed.');
    } finally {
      setBusyId(null);
    }
  };

  const handleRenameConfirm = async (nextName: string) => {
    if (!token || dialog?.type !== 'rename') return;
    const item = dialog.item;
    if (nextName === item.name) {
      setDialog(null);
      return;
    }

    await runItemAction(item.id, () => itemsApi.update(token, item.id, { name: nextName }));
    setDialog(null);
  };

  const handleMoveConfirm = async (raw: string) => {
    if (!token || dialog?.type !== 'move') return;
    const parentId = Number(raw);
    if (Number.isNaN(parentId) || parentId <= 0) {
      setError('Parent id must be a positive number.');
      return;
    }

    const item = dialog.item;
    await runItemAction(item.id, () => itemsApi.move(token, item.id, { parentId }));
    setDialog(null);
  };

  const handleDeactivateConfirm = async () => {
    if (!token || dialog?.type !== 'deactivate') return;
    const item = dialog.item;
    await runItemAction(item.id, () => itemsApi.deactivate(token, item.id));
    setDialog(null);
  };

  const handleDeleteConfirm = async () => {
    if (!token || dialog?.type !== 'delete') return;
    const item = dialog.item;
    const nextPage = items.length === 1 && page > 1 ? page - 1 : page;

    await runItemAction(
      item.id,
      () => itemsApi.delete(token, item.id),
      async () => {
        setPage(nextPage);
        await loadItems(nextPage, { silent: true });
      },
    );
    setDialog(null);
  };

  return (
    <div className="items-page">
      <div className="panel">
        <div className="panel-header row-between">
          <div>
            <h2>Items</h2>
            <p className="muted row-gap">
              <span>Newest first</span>
              <span className={`badge ${pushStatusClass(pushStatus)}`}>
                SignalR: {pushStatusLabel(pushStatus)}
              </span>
              {refreshing ? <span>Syncing…</span> : null}
            </p>
          </div>
          <button
            type="button"
            className="btn btn-ghost"
            disabled={refreshing}
            onClick={() => void loadItems(page, { silent: true })}
          >
            Refresh
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form className="inline-form" onSubmit={handleCreate}>
          <label className="field compact">
            <span>Parent</span>
            <select
              value={createParentId}
              onChange={(e) => setCreateParentId(e.target.value)}
              required
              disabled={loadingParents || parentOptions.length === 0}
            >
              {loadingParents ? (
                <option value="">Loading parents…</option>
              ) : parentOptions.length === 0 ? (
                <option value="">No parent items yet</option>
              ) : (
                parentOptions.map((item) => (
                  <option key={item.id} value={item.id}>
                    {formatParentOption(item)}
                  </option>
                ))
              )}
            </select>
          </label>
          <label className="field compact grow">
            <span>Name</span>
            <input
              value={createName}
              onChange={(e) => setCreateName(e.target.value)}
              placeholder="New item name"
              required
              disabled={parentOptions.length === 0}
            />
          </label>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={loadingParents || parentOptions.length === 0}
          >
            Create
          </button>
        </form>
        <p className="muted form-hint">
          Parents must exist in your tenant. Item IDs are not shared across tenants.
        </p>
      </div>

      <div className="panel">
        {loading ? (
          <p className="muted">Loading items…</p>
        ) : items.length === 0 ? (
          <p className="muted">
            No items yet. Each tenant starts with a Root item — refresh if you just registered.
          </p>
        ) : (
          <>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Name</th>
                  <th>Parent</th>
                  <th>Created</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className={!item.isActive ? 'inactive' : ''}>
                    <td>{item.id}</td>
                    <td>{item.name}</td>
                    <td>{item.parentId}</td>
                    <td>{formatCreatedAt(item.createdAtUtc)}</td>
                    <td>
                      <span className={`badge ${item.isActive ? 'badge-success' : 'badge-muted'}`}>
                        {item.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="actions">
                      <button
                        type="button"
                        className="btn btn-sm"
                        disabled={busyId === item.id}
                        onClick={() => setDialog({ type: 'rename', item })}
                      >
                        Rename
                      </button>
                      {isTenantAdmin && (
                        <>
                          <button
                            type="button"
                            className="btn btn-sm"
                            disabled={busyId === item.id}
                            onClick={() => setDialog({ type: 'move', item })}
                          >
                            Move
                          </button>
                          <button
                            type="button"
                            className="btn btn-sm"
                            disabled={busyId === item.id || !item.isActive}
                            onClick={() => setDialog({ type: 'deactivate', item })}
                          >
                            Deactivate
                          </button>
                          <button
                            type="button"
                            className="btn btn-sm btn-danger"
                            disabled={busyId === item.id}
                            onClick={() => setDialog({ type: 'delete', item })}
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="pagination">
              <span className="muted">
                {totalCount} item{totalCount === 1 ? '' : 's'} · page {page} of {totalPages}
              </span>
              <div className="pagination-actions">
                <button
                  type="button"
                  className="btn btn-sm"
                  disabled={page <= 1 || refreshing}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                >
                  Previous
                </button>
                <button
                  type="button"
                  className="btn btn-sm"
                  disabled={page >= totalPages || refreshing}
                  onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
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
        title="Rename item"
        label="New name"
        initialValue={dialog?.type === 'rename' ? dialog.item.name : ''}
        onConfirm={(value) => void handleRenameConfirm(value)}
        onCancel={() => setDialog(null)}
      />

      <PromptDialog
        open={dialog?.type === 'move'}
        title="Move item"
        label="New parent id"
        initialValue={dialog?.type === 'move' ? String(dialog.item.parentId) : ''}
        onConfirm={(value) => void handleMoveConfirm(value)}
        onCancel={() => setDialog(null)}
      />

      <ConfirmDialog
        open={dialog?.type === 'deactivate'}
        title="Deactivate item"
        message={dialog?.type === 'deactivate' ? `Deactivate "${dialog.item.name}"?` : ''}
        confirmLabel="Deactivate"
        onConfirm={() => void handleDeactivateConfirm()}
        onCancel={() => setDialog(null)}
      />

      <ConfirmDialog
        open={dialog?.type === 'delete'}
        title="Delete item"
        message={dialog?.type === 'delete' ? `Delete "${dialog.item.name}" permanently?` : ''}
        confirmLabel="Delete"
        tone="danger"
        onConfirm={() => void handleDeleteConfirm()}
        onCancel={() => setDialog(null)}
      />
    </div>
  );
}

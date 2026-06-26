import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { authApi, queuesApi, ticketsApi } from '../api';
import { useAccessToken, useAuth, useIsTenantAdmin } from '../auth/AuthContext';
import { ConfirmDialog, SelectDialog } from '../components/Modal';
import { RemotePushFlash } from '../components/RemotePushFlash';
import { useTicketsPush, type PushConnectionStatus } from '../hooks/useSupportDeskPush';
import { useRemotePushHighlights } from '../hooks/useRemotePushHighlights';
import type {
  ChangeNotificationDto,
  Queue,
  Ticket,
  TenantUser,
  TicketPriority,
  TicketStatus,
} from '../types';
import {
  PushOperation,
  applyDeletePatch,
  applyUpsertPatch,
  parseEntityNumericId,
  sortTicketsNewestFirst,
} from '../utils/pushListPatch';

const PAGE_SIZE = 20;
const QUEUE_OPTIONS_PAGE_SIZE = 100;

type DialogState =
  | { type: 'assign'; ticket: Ticket }
  | { type: 'delete'; ticket: Ticket }
  | null;

const STATUS_LABELS: Record<TicketStatus, string> = {
  0: 'New',
  1: 'Assigned',
  2: 'In progress',
  3: 'Resolved',
  4: 'Closed',
};

const PRIORITY_LABELS: Record<TicketPriority, string> = {
  0: 'Low',
  1: 'Normal',
  2: 'High',
};

function formatDateTime(value: string) {
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

function TicketStatusPill({ status }: { status: TicketStatus }) {
  const tone = status === 4 ? 'off' : status === 3 ? 'live' : 'warn';
  return (
    <span className="status-pill" data-tone={tone}>
      <span className="status-dot" aria-hidden />
      {STATUS_LABELS[status]}
    </span>
  );
}

function queueName(queues: Queue[], queueId: number) {
  return queues.find((q) => q.id === queueId)?.name ?? `#${queueId}`;
}

function formatUserLabel(users: TenantUser[], userId: number) {
  const user = users.find((u) => u.userId === userId);
  if (!user) return `user ${userId}`;
  return user.displayName ? `${user.displayName} (@${user.userName})` : user.userName;
}

function workflowHint(ticket: Ticket, isTenantAdmin: boolean) {
  if (ticket.status === 4) return 'This ticket is closed.';
  if (ticket.assigneeUserId == null) {
    return isTenantAdmin
      ? 'Assign an agent, then click Start progress.'
      : 'Waiting for a tenant admin to assign an agent.';
  }
  if (ticket.status === 0 || ticket.status === 1) return 'Click Start progress to begin work.';
  if (ticket.status === 2) return 'Click Resolve when the issue is fixed.';
  if (ticket.status === 3) return 'Click Close to archive this ticket.';
  return null;
}

export function TicketsPage() {
  const token = useAccessToken();
  const { session } = useAuth();
  const isTenantAdmin = useIsTenantAdmin();
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [tableSyncing, setTableSyncing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [selectedTicket, setSelectedTicket] = useState<Ticket | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [commentBody, setCommentBody] = useState('');

  const [queueOptions, setQueueOptions] = useState<Queue[]>([]);
  const [loadingQueues, setLoadingQueues] = useState(true);
  const [tenantUsers, setTenantUsers] = useState<TenantUser[]>([]);
  const [loadingTenantUsers, setLoadingTenantUsers] = useState(true);
  const [createQueueId, setCreateQueueId] = useState('');
  const [createSubject, setCreateSubject] = useState('');
  const [createDescription, setCreateDescription] = useState('');
  const [createPriority, setCreatePriority] = useState<TicketPriority>(1);

  const { markOwnAction, noteRemotePush, clearHighlights, isHighlighted } = useRemotePushHighlights();
  const loadSeqRef = useRef(0);
  const pageRef = useRef(page);
  const totalCountRef = useRef(totalCount);
  const selectedIdRef = useRef(selectedId);

  pageRef.current = page;
  totalCountRef.current = totalCount;
  selectedIdRef.current = selectedId;

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const loadQueueOptions = useCallback(async () => {
    if (!token) return;
    setLoadingQueues(true);
    try {
      const data = await queuesApi.list(token, { page: 1, pageSize: QUEUE_OPTIONS_PAGE_SIZE });
      const active = data.items.filter((q) => q.isActive).sort((a, b) => a.id - b.id);
      setQueueOptions(active);
      setCreateQueueId((current) => {
        if (current && active.some((q) => q.id === Number(current))) return current;
        return active.length > 0 ? String(active[0].id) : '';
      });
    } catch {
      setQueueOptions([]);
    } finally {
      setLoadingQueues(false);
    }
  }, [token]);

  const loadTenantUsers = useCallback(async () => {
    if (!token) return;
    setLoadingTenantUsers(true);
    try {
      const users = await authApi.listUsers(token);
      setTenantUsers(users);
    } catch {
      setTenantUsers([]);
    } finally {
      setLoadingTenantUsers(false);
    }
  }, [token]);

  const loadTickets = useCallback(async (targetPage = page, options?: { silent?: boolean }) => {
    if (!token) return;
    const seq = ++loadSeqRef.current;
    setError(null);
    if (options?.silent) setTableSyncing(true);
    else setLoading(true);

    try {
      const data = await ticketsApi.list(token, { page: targetPage, pageSize: PAGE_SIZE });
      if (seq !== loadSeqRef.current) return;
      setTickets(data.items);
      setTotalCount(data.totalCount);
      totalCountRef.current = data.totalCount;
      setPage(data.page);
      pageRef.current = data.page;
    } catch (err) {
      if (seq !== loadSeqRef.current) return;
      setError(err instanceof Error ? err.message : 'Failed to load tickets.');
    } finally {
      if (seq === loadSeqRef.current) {
        setLoading(false);
        setTableSyncing(false);
      }
    }
  }, [token, page]);

  const loadTicketDetail = useCallback(async (ticketId: number) => {
    if (!token) return;
    setDetailLoading(true);
    try {
      const ticket = await ticketsApi.get(token, ticketId);
      if (selectedIdRef.current === ticketId) {
        setSelectedTicket(ticket);
      }
    } catch (err) {
      if (selectedIdRef.current === ticketId) {
        setError(err instanceof Error ? err.message : 'Failed to load ticket.');
        setSelectedTicket(null);
      }
    } finally {
      if (selectedIdRef.current === ticketId) {
        setDetailLoading(false);
      }
    }
  }, [token]);

  useEffect(() => {
    void loadTickets(page);
  }, [page, token]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    void loadQueueOptions();
  }, [loadQueueOptions]);

  useEffect(() => {
    void loadTenantUsers();
  }, [loadTenantUsers]);

  useEffect(() => {
    if (selectedId == null) {
      setSelectedTicket(null);
      return;
    }
    void loadTicketDetail(selectedId);
  }, [selectedId, loadTicketDetail]);

  const reloadTable = useCallback(async (targetPage = pageRef.current) => {
    if (!token) return;
    setTableSyncing(true);
    try {
      const data = await ticketsApi.list(token, { page: targetPage, pageSize: PAGE_SIZE });
      setTickets(data.items);
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
      setTickets((prev) => {
        const result = applyDeletePatch(prev, totalCountRef.current, entityId);
        if (result.patched) {
          totalCountRef.current = result.totalCount;
          setTotalCount(result.totalCount);
        }
        return result.items;
      });
      if (selectedIdRef.current === entityId) {
        setSelectedId(null);
      }
      return;
    }

    setTableSyncing(true);
    try {
      const ticket = await ticketsApi.get(token, entityId);
      let needsFallback = false;

      setTickets((prev) => {
        const result = applyUpsertPatch(
          prev,
          totalCountRef.current,
          pageRef.current,
          PAGE_SIZE,
          ticket,
          sortTicketsNewestFirst,
        );
        totalCountRef.current = result.totalCount;
        setTotalCount(result.totalCount);
        needsFallback = !result.patched && pageRef.current === 1;
        return result.patched ? result.items : prev;
      });

      if (needsFallback) {
        await reloadTable(pageRef.current);
      }

      if (selectedIdRef.current === entityId) {
        setSelectedTicket(ticket);
      }

      noteRemotePush(entityId);
    } catch {
      await reloadTable(pageRef.current);
    } finally {
      setTableSyncing(false);
    }
  }, [token, reloadTable, noteRemotePush]);

  const pushStatus = useTicketsPush(token, session?.tenantId, handlePushUpdate);

  const handleOpenTicket = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!token || !session || !createQueueId) return;
    setError(null);
    try {
      const newId = await ticketsApi.open(token, {
        queueId: Number(createQueueId),
        subject: createSubject.trim(),
        description: createDescription.trim(),
        priority: createPriority,
        reporterUserId: session.userId,
      });
      markOwnAction(newId);
      setCreateSubject('');
      setCreateDescription('');
      setCreatePriority(1);
      setPage(1);
      await loadTickets(1);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to open ticket.');
    }
  };

  const runTicketAction = async (
    ticketId: number,
    action: () => Promise<void>,
    onSuccess?: () => void,
  ) => {
    markOwnAction(ticketId);
    setBusyId(ticketId);
    setError(null);
    try {
      await action();
      if (onSuccess) await onSuccess();
      else {
        await loadTickets(page, { silent: true });
        if (selectedIdRef.current === ticketId) {
          await loadTicketDetail(ticketId);
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed.');
    } finally {
      setBusyId(null);
    }
  };

  const handleAddComment = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!token || !session || !selectedTicket || !commentBody.trim()) return;
    const ticketId = selectedTicket.id;
    await runTicketAction(
      ticketId,
      () => ticketsApi.addComment(token, ticketId, {
        authorUserId: session.userId,
        body: commentBody.trim(),
      }),
      async () => {
        setCommentBody('');
        await loadTicketDetail(ticketId);
        await loadTickets(page, { silent: true });
      },
    );
  };

  const handleAssignConfirm = async (raw: string) => {
    if (!token || dialog?.type !== 'assign') return;
    const assigneeUserId = Number(raw);
    if (Number.isNaN(assigneeUserId) || assigneeUserId <= 0) return;
    const ticket = dialog.ticket;
    await runTicketAction(ticket.id, () =>
      ticketsApi.assign(token, ticket.id, { assigneeUserId }),
    );
    setDialog(null);
  };

  const assigneeOptions = tenantUsers.map((user) => ({
    value: String(user.userId),
    label: user.displayName
      ? `${user.displayName} (@${user.userName})`
      : `${user.userName} · ${user.email}`,
  }));

  const handleDeleteConfirm = async () => {
    if (!token || dialog?.type !== 'delete') return;
    const ticket = dialog.ticket;
    const nextPage = tickets.length === 1 && page > 1 ? page - 1 : page;
    await runTicketAction(
      ticket.id,
      () => ticketsApi.delete(token, ticket.id),
      async () => {
        setSelectedId(null);
        setPage(nextPage);
        await loadTickets(nextPage, { silent: true });
      },
    );
    setDialog(null);
  };

  const selectTicket = (ticketId: number) => {
    setSelectedId((current) => (current === ticketId ? null : ticketId));
  };

  const detail = selectedTicket;
  const detailBusy = detail != null && busyId === detail.id;

  return (
    <div className="tickets-page">
      <div className="panel">
        <div className="panel-header row-between">
          <div>
            <h2>Tickets</h2>
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
              void loadTickets(page, { silent: true });
              if (selectedId != null) void loadTicketDetail(selectedId);
            }}
          >
            Refresh
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form className="inline-form" onSubmit={handleOpenTicket}>
          <label className="field compact">
            <span>Queue</span>
            <select
              value={createQueueId}
              onChange={(e) => setCreateQueueId(e.target.value)}
              required
              disabled={loadingQueues || queueOptions.length === 0}
            >
              {loadingQueues ? (
                <option value="">Loading queues…</option>
              ) : queueOptions.length === 0 ? (
                <option value="">No active queues</option>
              ) : (
                queueOptions.map((queue) => (
                  <option key={queue.id} value={queue.id}>
                    {queue.name}
                  </option>
                ))
              )}
            </select>
          </label>
          <label className="field compact grow">
            <span>Subject</span>
            <input
              value={createSubject}
              onChange={(e) => setCreateSubject(e.target.value)}
              placeholder="Brief summary"
              required
              disabled={queueOptions.length === 0}
            />
          </label>
          <label className="field compact grow">
            <span>Description</span>
            <input
              value={createDescription}
              onChange={(e) => setCreateDescription(e.target.value)}
              placeholder="Details (optional)"
              disabled={queueOptions.length === 0}
            />
          </label>
          <label className="field compact">
            <span>Priority</span>
            <select
              value={createPriority}
              onChange={(e) => setCreatePriority(Number(e.target.value) as TicketPriority)}
              disabled={queueOptions.length === 0}
            >
              <option value={0}>Low</option>
              <option value={1}>Normal</option>
              <option value={2}>High</option>
            </select>
          </label>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={loadingQueues || queueOptions.length === 0}
          >
            Open ticket
          </button>
        </form>
      </div>

      <div className={`panel panel-table${tableSyncing ? ' panel-table-syncing' : ''}`}>
        {tableSyncing && !loading ? (
          <p className="table-sync-hint muted mono">updating…</p>
        ) : null}

        {loading ? (
          <p className="muted">Loading tickets…</p>
        ) : tickets.length === 0 ? (
          <p className="muted">No tickets yet.</p>
        ) : (
          <>
            <table className="data-table data-table-selectable">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Subject</th>
                  <th>Queue</th>
                  <th>Status</th>
                  <th>Priority</th>
                  <th>Created</th>
                  <th className="remote-col" aria-hidden />
                </tr>
              </thead>
              <tbody>
                {tickets.map((ticket) => (
                  <tr
                    key={ticket.id}
                    className={[
                      selectedId === ticket.id ? 'selected' : '',
                      ticket.status === 4 ? 'inactive' : '',
                    ].filter(Boolean).join(' ') || undefined}
                    onClick={() => selectTicket(ticket.id)}
                  >
                    <td className="mono tabular">{ticket.id}</td>
                    <td>{ticket.subject}</td>
                    <td>{queueName(queueOptions, ticket.queueId)}</td>
                    <td>
                      <TicketStatusPill status={ticket.status} />
                    </td>
                    <td>{PRIORITY_LABELS[ticket.priority]}</td>
                    <td>{formatDateTime(ticket.createdAtUtc)}</td>
                    <td className="remote-col">
                      <RemotePushFlash show={isHighlighted(ticket.id)} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className="pagination">
              <span className="muted mono tabular">
                {totalCount} ticket{totalCount === 1 ? '' : 's'} · page {page} of {totalPages}
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

      {selectedId != null && (
        <div className="panel ticket-detail">
          {detailLoading && !detail ? (
            <p className="muted">Loading ticket…</p>
          ) : !detail ? (
            <p className="muted">Ticket not found.</p>
          ) : (
            <>
              <div className="panel-header row-between">
                <div>
                  <h3>
                    #{detail.id} · {detail.subject}
                  </h3>
                  <p className="muted row-gap">
                    <TicketStatusPill status={detail.status} />
                    <span>{PRIORITY_LABELS[detail.priority]} priority</span>
                    <span>queue {queueName(queueOptions, detail.queueId)}</span>
                  </p>
                  <p className="muted form-hint">{workflowHint(detail, isTenantAdmin)}</p>
                </div>
                <div className="actions">
                  {isTenantAdmin && detail.status !== 4 && (
                    <button
                      type="button"
                      className="btn btn-sm"
                      disabled={detailBusy}
                      onClick={() => setDialog({ type: 'assign', ticket: detail })}
                    >
                      Assign
                    </button>
                  )}
                  {(detail.status === 0 || detail.status === 1) && detail.assigneeUserId != null && token && (
                    <button
                      type="button"
                      className="btn btn-sm"
                      disabled={detailBusy}
                      onClick={() =>
                        void runTicketAction(detail.id, () => ticketsApi.startProgress(token, detail.id))
                      }
                    >
                      Start progress
                    </button>
                  )}
                  {detail.status === 2 && token && (
                    <button
                      type="button"
                      className="btn btn-sm"
                      disabled={detailBusy}
                      onClick={() =>
                        void runTicketAction(detail.id, () => ticketsApi.resolve(token, detail.id))
                      }
                    >
                      Resolve
                    </button>
                  )}
                  {detail.status === 3 && token && (
                    <button
                      type="button"
                      className="btn btn-sm"
                      disabled={detailBusy}
                      onClick={() =>
                        void runTicketAction(detail.id, () => ticketsApi.close(token, detail.id))
                      }
                    >
                      Close
                    </button>
                  )}
                  {isTenantAdmin && (
                    <button
                      type="button"
                      className="btn btn-sm btn-danger"
                      disabled={detailBusy}
                      onClick={() => setDialog({ type: 'delete', ticket: detail })}
                    >
                      Delete
                    </button>
                  )}
                </div>
              </div>

              <p className="ticket-description">{detail.description || '—'}</p>
              <p className="muted mono tabular form-hint">
                reporter {formatUserLabel(tenantUsers, detail.reporterUserId)}
                {detail.assigneeUserId != null
                  ? ` · assignee ${formatUserLabel(tenantUsers, detail.assigneeUserId)}`
                  : ''}
                {' · '}updated {formatDateTime(detail.updatedAtUtc)}
              </p>

              <div className="ticket-comments">
                <h4>Comments</h4>
                {detail.comments.length === 0 ? (
                  <p className="muted">No comments yet.</p>
                ) : (
                  <ul className="comment-list">
                    {detail.comments.map((comment) => (
                      <li key={comment.id} className="comment-item">
                        <div className="comment-meta muted mono tabular">
                          {formatUserLabel(tenantUsers, comment.authorUserId)} ·{' '}
                          {formatDateTime(comment.createdAtUtc)}
                        </div>
                        <p>{comment.body}</p>
                      </li>
                    ))}
                  </ul>
                )}

                {detail.status !== 4 && (
                  <form className="inline-form" onSubmit={handleAddComment}>
                    <label className="field grow">
                      <span>Add comment</span>
                      <input
                        value={commentBody}
                        onChange={(e) => setCommentBody(e.target.value)}
                        placeholder="Write a comment…"
                        required
                        disabled={detailBusy}
                      />
                    </label>
                    <button type="submit" className="btn btn-primary" disabled={detailBusy}>
                      Post
                    </button>
                  </form>
                )}
              </div>
            </>
          )}
        </div>
      )}

      <SelectDialog
        open={dialog?.type === 'assign'}
        title="Assign ticket"
        label="Assignee"
        options={assigneeOptions}
        initialValue={
          dialog?.type === 'assign' && dialog.ticket.assigneeUserId != null
            ? String(dialog.ticket.assigneeUserId)
            : ''
        }
        loading={loadingTenantUsers}
        emptyMessage="No users in this tenant yet. Create users under Admin → Users."
        confirmLabel="Assign"
        onConfirm={(value) => void handleAssignConfirm(value)}
        onCancel={() => setDialog(null)}
      />

      <ConfirmDialog
        open={dialog?.type === 'delete'}
        title="Delete ticket"
        message={dialog?.type === 'delete' ? `Delete ticket #${dialog.ticket.id} permanently?` : ''}
        confirmLabel="Delete"
        tone="danger"
        onConfirm={() => void handleDeleteConfirm()}
        onCancel={() => setDialog(null)}
      />
    </div>
  );
}

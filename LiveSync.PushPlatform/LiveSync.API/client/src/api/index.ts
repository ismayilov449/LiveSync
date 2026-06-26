import { apiFetch } from './http';
import type {
  AddCommentRequest,
  AssignTicketRequest,
  AuthSession,
  ChangeQueueStats,
  CreateQueueRequest,
  CreateUserRequest,
  CreatedUserResponse,
  LoginRequest,
  OpenTicketRequest,
  PagedAuditEvents,
  PagedQueuesResponse,
  PagedTicketsResponse,
  Queue,
  RegisterRequest,
  Ticket,
  TenantUser,
  UpdateQueueRequest,
  UserProfile,
} from '../types';

const API_V1 = '/api/v1';

export const authApi = {
  login: (body: LoginRequest) =>
    apiFetch<AuthSession>(`${API_V1}/auth/login`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  register: (body: RegisterRequest) =>
    apiFetch<AuthSession>(`${API_V1}/auth/register`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  me: (token: string) =>
    apiFetch<UserProfile>(`${API_V1}/auth/me`, {}, token),

  listUsers: (token: string) =>
    apiFetch<TenantUser[]>(`${API_V1}/auth/users`, {}, token),

  createUser: (token: string, body: CreateUserRequest) =>
    apiFetch<CreatedUserResponse>(`${API_V1}/auth/users`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, token),
};

export const ticketsApi = {
  list: (
    token: string,
    options?: { queueId?: number; status?: number; page?: number; pageSize?: number },
  ) => {
    const params = new URLSearchParams();
    if (options?.queueId != null) params.set('queueId', String(options.queueId));
    if (options?.status != null) params.set('status', String(options.status));
    if (options?.page != null) params.set('page', String(options.page));
    if (options?.pageSize != null) params.set('pageSize', String(options.pageSize));
    const query = params.toString();
    return apiFetch<PagedTicketsResponse>(`${API_V1}/tickets${query ? `?${query}` : ''}`, {}, token);
  },

  get: (token: string, id: number) =>
    apiFetch<Ticket>(`${API_V1}/tickets/${id}`, {}, token),

  open: (token: string, body: OpenTicketRequest) =>
    apiFetch<number>(`${API_V1}/tickets`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, token),

  assign: (token: string, id: number, body: AssignTicketRequest) =>
    apiFetch<void>(`${API_V1}/tickets/${id}/assign`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }, token),

  addComment: (token: string, id: number, body: AddCommentRequest) =>
    apiFetch<void>(`${API_V1}/tickets/${id}/comments`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, token),

  startProgress: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/tickets/${id}/start-progress`, { method: 'POST' }, token),

  resolve: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/tickets/${id}/resolve`, { method: 'POST' }, token),

  close: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/tickets/${id}/close`, { method: 'POST' }, token),

  delete: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/tickets/${id}`, { method: 'DELETE' }, token),
};

export const queuesApi = {
  list: (token: string, options?: { page?: number; pageSize?: number }) => {
    const params = new URLSearchParams();
    if (options?.page != null) params.set('page', String(options.page));
    if (options?.pageSize != null) params.set('pageSize', String(options.pageSize));
    const query = params.toString();
    return apiFetch<PagedQueuesResponse>(`${API_V1}/queues${query ? `?${query}` : ''}`, {}, token);
  },

  get: (token: string, id: number) =>
    apiFetch<Queue>(`${API_V1}/queues/${id}`, {}, token),

  create: (token: string, body: CreateQueueRequest) =>
    apiFetch<number>(`${API_V1}/queues`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, token),

  update: (token: string, id: number, body: UpdateQueueRequest) =>
    apiFetch<void>(`${API_V1}/queues/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }, token),

  delete: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/queues/${id}`, { method: 'DELETE' }, token),

  deactivate: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/queues/${id}/deactivate`, { method: 'POST' }, token),
};

export const operationsApi = {
  changeQueue: (token: string) =>
    apiFetch<ChangeQueueStats>(`${API_V1}/operations/change-queue`, {}, token),
};

export const auditApi = {
  list: (token: string, page = 1, pageSize = 20) => {
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    });
    return apiFetch<PagedAuditEvents>(`${API_V1}/audit?${params}`, {}, token);
  },
};

export const tenantApi = {
  suspend: (token: string) =>
    apiFetch<void>(`${API_V1}/tenants/suspend`, { method: 'POST' }, token),

  reactivate: (token: string) =>
    apiFetch<void>(`${API_V1}/tenants/reactivate`, { method: 'POST' }, token),
};

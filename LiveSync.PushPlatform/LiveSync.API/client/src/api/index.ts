import { apiFetch } from './http';
import type {
  AuthSession,
  ChangeQueueStats,
  CreateItemRequest,
  CreateUserRequest,
  CreatedUserResponse,
  Item,
  LoginRequest,
  MoveItemRequest,
  PagedAuditEvents,
  PagedItemsResponse,
  RegisterRequest,
  UpdateItemRequest,
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

  createUser: (token: string, body: CreateUserRequest) =>
    apiFetch<CreatedUserResponse>(`${API_V1}/auth/users`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, token),
};

export const itemsApi = {
  list: (
    token: string,
    options?: { parentId?: number; page?: number; pageSize?: number },
  ) => {
    const params = new URLSearchParams();
    if (options?.parentId != null) params.set('parentId', String(options.parentId));
    if (options?.page != null) params.set('page', String(options.page));
    if (options?.pageSize != null) params.set('pageSize', String(options.pageSize));
    const query = params.toString();
    return apiFetch<PagedItemsResponse>(`${API_V1}/items${query ? `?${query}` : ''}`, {}, token);
  },

  get: (token: string, id: number) =>
    apiFetch<Item>(`${API_V1}/items/${id}`, {}, token),

  create: (token: string, body: CreateItemRequest) =>
    apiFetch<number>(`${API_V1}/items`, {
      method: 'POST',
      body: JSON.stringify(body),
    }, token),

  update: (token: string, id: number, body: UpdateItemRequest) =>
    apiFetch<void>(`${API_V1}/items/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }, token),

  delete: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/items/${id}`, { method: 'DELETE' }, token),

  deactivate: (token: string, id: number) =>
    apiFetch<void>(`${API_V1}/items/${id}/deactivate`, { method: 'POST' }, token),

  move: (token: string, id: number, body: MoveItemRequest) =>
    apiFetch<void>(`${API_V1}/items/${id}/parent`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }, token),
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

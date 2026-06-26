export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  tenantId: number;
  userId: number;
  userName: string;
}

export interface UserProfile {
  userId: number;
  tenantId: number;
  tenantName: string;
  tenantStatus: string;
  userName: string;
  email: string;
  displayName: string;
  roles: string[];
}

export interface ChangeQueueStats {
  pendingCount: number;
  deadLetterCount: number;
}

export interface AuditEvent {
  id: number;
  tenantId: number;
  userId: number;
  action: string;
  entityType: string;
  entityId: string | null;
  details: string | null;
  createdAtUtc: string;
}

export interface PagedAuditEvents {
  items: AuditEvent[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  password: string;
  displayName: string;
}

export interface CreatedUserResponse {
  userId: number;
  tenantId: number;
  userName: string;
  email: string;
  displayName: string;
}

export interface Item {
  id: number;
  tenantId: number;
  parentId: number;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface PagedItemsResponse {
  items: Item[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface LoginRequest {
  userName: string;
  password: string;
}

export interface RegisterRequest {
  tenantName: string;
  userName: string;
  email: string;
  password: string;
  displayName: string;
}

export interface CreateItemRequest {
  parentId: number;
  name: string;
}

export interface UpdateItemRequest {
  name: string;
}

export interface MoveItemRequest {
  parentId: number;
}

export interface ApiError {
  message: string;
}

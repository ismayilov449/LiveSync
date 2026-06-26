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

export interface TenantUser {
  userId: number;
  userName: string;
  email: string;
  displayName: string;
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

export interface ApiError {
  message: string;
}

export interface ChangeNotificationDto {
  operation: number;
  entity: {
    id: string;
    bucket: string;
  };
  change?: unknown;
}

export type TicketStatus = 0 | 1 | 2 | 3 | 4;
export type TicketPriority = 0 | 1 | 2;

export interface TicketComment {
  id: number;
  authorUserId: number;
  body: string;
  createdAtUtc: string;
}

export interface Ticket {
  id: number;
  tenantId: number;
  queueId: number;
  subject: string;
  description: string;
  status: TicketStatus;
  priority: TicketPriority;
  reporterUserId: number;
  assigneeUserId: number | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  comments: TicketComment[];
}

export interface PagedTicketsResponse {
  items: Ticket[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OpenTicketRequest {
  queueId: number;
  subject: string;
  description: string;
  priority: TicketPriority;
  reporterUserId: number;
}

export interface AssignTicketRequest {
  assigneeUserId: number;
}

export interface AddCommentRequest {
  authorUserId: number;
  body: string;
}

export interface Queue {
  id: number;
  tenantId: number;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface PagedQueuesResponse {
  items: Queue[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateQueueRequest {
  name: string;
}

export interface UpdateQueueRequest {
  name: string;
}

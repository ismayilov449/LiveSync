# Architecture

## Overview

LiveSync is a **multi-tenant support desk** where each organization (tenant) receives an isolated SQL Server database. A central **control plane** stores tenant registry, ASP.NET Identity users, audit events, and maps each tenant to its database name.

The business domain is **Support Desk**: agents collaborate on **tickets** organized into **queues**, with real-time sync when anyone opens, comments on, assigns, or advances ticket status.

## Components

### LiveSync.API

- REST API (`/api/v1/...`) with JWT authentication
- Serves the React SPA from `wwwroot/` (dark, compact UI with admin console)
- SignalR hub at `/hubs/push` for live updates
- Pushes **immediate** bucket-scoped SignalR notifications on ticket and queue mutations
- Prometheus metrics at `/metrics`; optional OTLP export
- Tenant lifecycle (`suspend` / `reactivate`), audit log, operations endpoints
- Per-tenant rate limiting on authenticated API traffic
- Does **not** run the change-detection poll loop (delegated to Worker by default)

### LiveSync.Worker

- Polls change queues across active tenant databases
- Processes domain events enqueued after ticket and queue mutations
- Moves failed entries to **dead-letter** after `MaxRetries`
- Updates Redis topic caches and pushes bucket-scoped SignalR notifications
- Runs subscription expiry cleanup
- Exposes `/metrics` and health endpoints on port **5260** (local dev)

### Control plane (`LiveSync_ControlPlane`)

| Table | Purpose |
|-------|---------|
| `Tenants` | Tenant registry, database name, status (`Provisioning`, `Active`, `Suspended`) |
| `AspNetUsers` | Users with `TenantId` foreign key |
| `AspNetRoles` | `TenantAdmin`, `TenantUser` |
| `AuditEvents` | Tenant-scoped administrative audit trail |

### Tenant databases (`LiveSync_Tenant_{id}`)

| Table | Purpose |
|-------|---------|
| `Queues` | Work streams per tenant (e.g. General, IT) |
| `Tickets` | Support tickets with status, priority, assignee |
| `TicketComments` | Comments owned by ticket aggregate |
| `ChangeQueue` | Outbox for real-time sync pipeline (`Pending` / `DeadLetter` status) |
| `IdempotencyRecords` | `Idempotency-Key` → created ticket id (per tenant) |

## Domain model (DDD)

| Aggregate | Responsibility |
|-----------|----------------|
| **Queue** | Flat work streams; deactivate blocked if open tickets exist |
| **Ticket** | Status machine, assignee, comments as child entities |

**Ticket lifecycle:** `New → Assigned → InProgress → Resolved → Closed`

Details: [adr/006-support-desk-aggregates.md](adr/006-support-desk-aggregates.md)

## Request flow — open ticket or manage queue

```
Client POST /api/v1/tickets or /api/v1/queues
  [optional Idempotency-Key on ticket open]
  → Command handler (MediatR)
  → Aggregate saved to tenant DB
  → Domain event
  → API: NotifyTenant*DomainEventHandler → SignalR PushUpdate (immediate, bucket-scoped)
  → Enqueued in ChangeQueue (Pending)
  → AuditService records action
  → Worker picks up change
  → *ChangeHandler updates Redis + SignalR PushUpdate (consistency)
  → On repeated failure: status → DeadLetter
  → Connected clients patch table rows (SPA)
```

See [real-time-sync.md](real-time-sync.md) for sequence diagrams.

## Subscription flow

```
Client connects to /hubs/push?access_token=...
  → FindAndSubscribe(bucket: Ticket|Queue, filter: {bucket}.TenantId == N)
  → Connection added to group tenant:{tenantId}:bucket:{ticket|queue}
  → Subscription stored in Redis (Polly retry/circuit breaker on Redis calls)
  → Initial cache snapshot returned
  → PushUpdate on subsequent changes for that bucket
```

## Middleware pipeline (API)

| Order | Middleware | Role |
|-------|------------|------|
| — | Exception handling | RFC 7807 ProblemDetails |
| — | Authentication / Authorization | JWT + RBAC |
| — | Rate limiter | Per-IP auth limits; per-tenant API limits |
| — | `TenantStatusMiddleware` | Blocks suspended tenants (403); allows `/tenants/reactivate` |
| — | Tenant access validation | User belongs to JWT tenant |

## Cross-cutting concerns

| Concern | Implementation |
|---------|----------------|
| Validation | FluentValidation + MediatR pipeline |
| Errors | ProblemDetails via `ExceptionHandlingMiddleware` (`TenantSuspendedException` → 403) |
| Logging | Serilog with correlation ID enrichment |
| Tracing / metrics | OpenTelemetry (ASP.NET, EF Core, HTTP); Prometheus `/metrics` |
| Custom metrics | `livesync_change_queue_depth`, `dead_letter_depth`, change counters, SignalR pushes |
| Rate limiting | Auth: 30 req/min per IP; API: `RateLimiting:TenantPermitLimit` per tenant (default 200/min) |
| Idempotency | `Idempotency-Key` header on `POST /tickets` |
| Audit | `AuditEvents` in control plane; admin UI + `GET /api/v1/audit` |
| Resilience | EF `EnableRetryOnFailure` on SQL; Polly on Redis operations |
| Versioning | URL path `api/v{version}/...` |

## React SPA routes

| Route | Access | Purpose |
|-------|--------|---------|
| `/tickets` | Authenticated | Ticket list, open ticket, detail panel (comments + workflow), SignalR live status, row-patch on push |
| `/queues` | Authenticated | Queue management, bucket-scoped SignalR |
| `/profile` | Authenticated | Account profile (user + org metadata) |
| `/admin/overview` | TenantAdmin | Queue stats, org status |
| `/admin/users` | TenantAdmin | Invite users to tenant |
| `/admin/audit` | TenantAdmin | Paginated audit log |
| `/admin/settings` | TenantAdmin | Suspend / reactivate tenant |
| `/about` | Authenticated | Architecture summary |
| `/login`, `/register` | Guest | Auth |

Default redirect after login: `/tickets`.

Client details: [client-development.md](client-development.md)

## Layering

```
Domain          → Queue, Ticket aggregates, events, repository interfaces
Application     → CQRS handlers, subscription manager, ports, metrics
Infrastructure  → EF Core, Redis, SignalR notifier, tenancy, audit, idempotency
API / Worker    → composition root, hosting
```

| Layer | Examples |
|-------|----------|
| **Domain** | `Queue`, `Ticket`, `TicketComment`, status machine, domain events |
| **Application** | `OpenTicketCommandHandler`, `AssignTicketCommandHandler`, `SubscriptionManager`, `IRealTimeNotifier` |
| **Infrastructure** | `TicketRepository`, `QueueRepository`, `RedisSubscriptionStore`, `AuditService` |
| **API** | `TicketsController`, `QueuesController`, `AuthController`, middleware, JWT, SPA |
| **Worker** | `ChangeDetectionHostedService`, bucket change handlers, queue metrics |

## Extending

See [extending-the-platform.md](extending-the-platform.md) for adding new aggregates.

## Observability stack (optional local)

See root README **Observability** section. Compose profile under `observability/` runs Prometheus, Grafana, and an OTLP collector.

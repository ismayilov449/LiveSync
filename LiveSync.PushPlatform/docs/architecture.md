# Architecture

## Overview

LiveSync is a multi-tenant platform where each organization (tenant) receives an isolated SQL Server database. A central **control plane** stores tenant registry, ASP.NET Identity users, audit events, and maps each tenant to its database name.

## Components

### LiveSync.API

- REST API (`/api/v1/...`) with JWT authentication
- Serves the React SPA from `wwwroot/` (dark, compact UI with admin console)
- SignalR hub at `/hubs/push` for live updates
- Pushes **immediate** tenant-wide SignalR notifications on item mutations
- Prometheus metrics at `/metrics`; optional OTLP export
- Tenant lifecycle (`suspend` / `reactivate`), audit log, operations endpoints
- Per-tenant rate limiting on authenticated API traffic
- Does **not** run the change-detection poll loop (delegated to Worker by default)

### LiveSync.Worker

- Polls change queues across active tenant databases
- Processes domain events enqueued after item mutations
- Moves failed entries to **dead-letter** after `MaxRetries`
- Updates Redis topic caches and pushes SignalR notifications
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
| `Items` | Tenant item hierarchy |
| `ChangeQueue` | Outbox for real-time sync pipeline (`Pending` / `DeadLetter` status) |
| `IdempotencyRecords` | `Idempotency-Key` → created item id (per tenant) |

## Request flow — create item

```
Client POST /api/v1/items
  [optional Idempotency-Key header]
  → CreateItemCommandHandler
  → Item saved to tenant DB
  → ItemCreatedDomainEvent
  → API: NotifyTenantItemDomainEventHandler → SignalR PushUpdate (immediate)
  → Enqueued in ChangeQueue (Pending)
  → AuditService records "create" action
  → Worker picks up change
  → ItemChangeHandler updates Redis + SignalR PushUpdate (consistency)
  → On repeated failure: status → DeadLetter
  → Connected clients refresh
```

See [real-time-sync.md](real-time-sync.md) for sequence diagrams.

## Subscription flow

```
Client connects to /hubs/push?access_token=...
  → Connection added to group tenant:{tenantId}
  → FindAndSubscribe(bucket: Item, filter: item.TenantId == N)
  → Subscription stored in Redis (Polly retry/circuit breaker on Redis calls)
  → Initial cache snapshot returned
  → PushUpdate on subsequent changes matching filter
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
| Custom metrics | `livesync.change_queue.depth`, `dead_letter_depth`, change counters, SignalR pushes |
| Rate limiting | Auth: 30 req/min per IP; API: `RateLimiting:TenantPermitLimit` per tenant (default 200/min) |
| Idempotency | `Idempotency-Key` header on `POST /items` |
| Audit | `AuditEvents` in control plane; admin UI + `GET /api/v1/audit` |
| Resilience | EF `EnableRetryOnFailure` on SQL; Polly on Redis operations |
| Versioning | URL path `api/v{version}/...` |

## React SPA routes

| Route | Access | Purpose |
|-------|--------|---------|
| `/items` | Authenticated | Item list, create, SignalR live status |
| `/profile` | Authenticated | Account profile (user + org metadata) |
| `/admin/overview` | TenantAdmin | Queue stats, org status |
| `/admin/users` | TenantAdmin | Invite users |
| `/admin/audit` | TenantAdmin | Paginated audit log |
| `/admin/settings` | TenantAdmin | Suspend / reactivate tenant |
| `/about` | Authenticated | Architecture summary |
| `/login`, `/register` | Guest | Auth |

## Layering

```
Domain          → entities, events, repository interfaces
Application     → CQRS handlers, subscription manager, ports, metrics
Infrastructure  → EF Core, Redis, SignalR notifier, tenancy, audit, idempotency
API / Worker    → composition root, hosting
```

## Observability stack (optional local)

See root README **Observability** section. Compose profile under `observability/` runs Prometheus, Grafana, and an OTLP collector.

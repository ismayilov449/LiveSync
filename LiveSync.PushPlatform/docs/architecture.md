# Architecture

## Overview

LiveSync is a multi-tenant platform where each organization (tenant) receives an isolated SQL Server database. A central **control plane** stores tenant registry, ASP.NET Identity users, and maps each tenant to its database name.

## Components

### LiveSync.API

- REST API (`/api/v1/...`) with JWT authentication
- Serves the React SPA from `wwwroot/`
- SignalR hub at `/hubs/push` for live updates
- Does **not** run change detection (delegated to Worker)

### LiveSync.Worker

- Polls change queues across active tenant databases
- Processes domain events enqueued after item mutations
- Updates Redis topic caches and pushes SignalR notifications
- Runs subscription expiry cleanup

### Control plane (`LiveSync_ControlPlane`)

| Table | Purpose |
|-------|---------|
| `Tenants` | Tenant registry, database name, status |
| `AspNetUsers` | Users with `TenantId` foreign key |
| `AspNetRoles` | `TenantAdmin`, `TenantUser` |

### Tenant databases (`LiveSync_Tenant_{id}`)

| Table | Purpose |
|-------|---------|
| `Items` | Tenant item hierarchy |
| `ChangeQueue` | Outbox for real-time sync pipeline |

## Request flow — create item

```
Client POST /api/v1/items
  → CreateItemCommandHandler
  → Item saved to tenant DB
  → ItemCreatedDomainEvent
  → Enqueued in ChangeQueue
  → Worker picks up change
  → ItemChangeHandler updates Redis + SignalR PushUpdate
  → Connected clients refresh
```

## Subscription flow

```
Client connects to /hubs/push?access_token=...
  → FindAndSubscribe(bucket: Item, filter: item.TenantId == N)
  → Subscription stored in Redis
  → Initial cache snapshot returned
  → PushUpdate on subsequent changes matching filter
```

## Cross-cutting concerns

| Concern | Implementation |
|---------|----------------|
| Validation | FluentValidation + MediatR pipeline |
| Errors | ProblemDetails via ExceptionHandlingMiddleware |
| Logging | Serilog with correlation ID enrichment |
| Tracing | OpenTelemetry (ASP.NET, EF Core, HTTP) |
| Rate limiting | Fixed window on `/api/v1/auth/*` |
| Versioning | URL path `api/v{version}/...` |

## Layering

```
Domain          → entities, events, repository interfaces
Application     → CQRS handlers, subscription manager, ports
Infrastructure  → EF Core, Redis, SignalR notifier, tenancy
API / Worker    → composition root, hosting
```

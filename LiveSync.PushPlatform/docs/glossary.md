# Glossary

Terms used across LiveSync docs and code.

| Term | Meaning |
|------|---------|
| **Control plane** | Central SQL database (`LiveSync_ControlPlane`) for tenants, Identity users, roles, and audit events. |
| **Tenant database** | Isolated SQL database per organization (`LiveSync_Tenant_{id}`) holding business data and outbox. |
| **Tenant data plane** | All per-tenant databases collectively; accessed only after JWT tenant resolution. |
| **Support Desk** | Bounded context: **Queues** (work streams) and **Tickets** (support requests with status lifecycle). |
| **Queue** | Aggregate root — flat list of work streams per tenant (e.g. "General", "IT"). Create, rename, deactivate, delete. |
| **Ticket** | Aggregate root — belongs to one queue; owns comments and enforces status transitions. |
| **TicketComment** | Entity **inside** the `Ticket` aggregate — no standalone comment aggregate or API. |
| **Ticket status** | `New → Assigned → InProgress → Resolved → Closed` — enforced in domain methods, not free-form edits. |
| **TopicBucket** | Domain enum (`Ticket = 1`, `Queue = 2`) routing real-time sync and change handlers to the correct module. |
| **ChangeQueue** | Per-tenant outbox table (`Pending` / `DeadLetter`) processed by the Worker. |
| **Dead-letter** | Change queue entry that exceeded `ChangeDetection:MaxRetries`; kept for operator inspection. |
| **Outbox** | Pattern: persist domain change in SQL first, process asynchronously via Worker. |
| **FindAndSubscribe** | SignalR hub method registering a filtered Redis subscription for a bucket + filter expression. |
| **PushUpdate** | SignalR client event carrying `ChangeNotificationDto` (operation, entity id, optional change payload). |
| **Bucket-scoped group** | SignalR group `tenant:{tenantId}:bucket:{ticket\|queue}` — push only to subscribers of that bucket. |
| **Immediate push** | API path: notify SignalR right after save (low latency). |
| **Worker push** | Background path: poll queue, update Redis cache, notify SignalR (consistency). |
| **TenantAdmin** | Role: assign/delete tickets, manage queues (deactivate/delete), invite users, admin console, suspend/reactivate. |
| **TenantUser** | Role: open tickets, comment, advance workflow (start/resolve/close); cannot assign, delete tickets, or access admin console. |
| **Idempotency-Key** | HTTP header on `POST /tickets` replaying the same open without duplicate rows. |
| **DynamicExpresso** | Library evaluating subscription filter strings (e.g. `ticket.TenantId == 1`). |
| **Row patch (Option B)** | Client updates one table row from push instead of refetching the full list. |
| **Remote push flash** | Red lightning icon on rows changed by another user/session (cleared on refresh or navigation). |
| **General queue** | Default queue auto-created when a tenant is provisioned (replaces legacy "root item" bootstrap). |

See also: [architecture.md](architecture.md), [real-time-sync.md](real-time-sync.md), [tenancy.md](tenancy.md), [adr/006-support-desk-aggregates.md](adr/006-support-desk-aggregates.md).

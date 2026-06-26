# Resume & LinkedIn bullets — Solution architect

Copy-paste bullets for CV, LinkedIn, or GitHub profile. **Customize** with your name and metrics where marked.

---

## Solution architect / senior engineer

- Architected **LiveSync**, a multi-tenant B2B **support desk** SaaS reference platform using **database-per-tenant isolation**, DDD aggregates (Queues + Tickets), CQRS, and real-time **SignalR** sync — documented with C4 views, ADRs, and NFRs ([solution-architecture.md](solution-architecture.md)).

- Designed **control plane vs tenant data plane** split: central SQL database for Identity, tenant registry, and audit; isolated `LiveSync_Tenant_{id}` databases for queues, tickets, comments, outbox, and idempotency records.

- Modeled **Support Desk domain** with `Ticket` status machine (`New → Closed`), comment entities inside the ticket aggregate, and queue deactivate rules enforced in domain code ([ADR 006](adr/006-support-desk-aggregates.md)).

- Defined **API + Worker** deployment topology: stateless REST/SignalR front tier; background workers for change detection, dead-letter handling, and subscription expiry — independently scalable failure domains (ADR-002).

- Implemented **transactional outbox** pattern via per-tenant `ChangeQueue` with worker batch claiming, retries, dead-letter status, and Redis-backed subscription cache (ADR-003).

- Solved cross-user live sync with **bucket-scoped SignalR groups** (`ticket` / `queue`) and Redis backplane (ADR 004, amended by ADR 005).

- Delivered **platform operability**: Prometheus custom metrics (queue depth, dead-letter, SignalR pushes), OTLP export, optional Grafana/Prometheus compose stack, health probes.

- Built **tenant lifecycle and governance**: suspend/reactivate API, middleware enforcement, audit log for administrative actions, per-tenant rate limiting, and `Idempotency-Key` on ticket open.

- Established **security architecture**: JWT with tenant claims, ASP.NET Identity RBAC (`TenantAdmin` / `TenantUser`), tenant access validation, Polly resilience on Redis, EF retry on SQL.

- Specified **non-functional requirements** for isolation, latency, operability (health, Serilog, correlation IDs, OpenTelemetry).

- Delivered **CI/CD pipeline** (GitHub Actions): client lint + Vitest + build, unit + Testcontainers integration tests (31 total automated), vulnerability scan, Docker images.

---

## .NET / backend (shorter bullets)

- Built .NET 10 API with **MediatR CQRS**, FluentValidation pipeline, EF Core multi-tenant routing, and versioned REST (`/api/v1`).

- Integrated **ASP.NET Identity + JWT** with tenant-scoped authorization, suspend/reactivate lifecycle, and database-per-tenant provisioning (`TenantProvisioner`).

- Implemented **ticket workflow** commands: open, assign, comment, start-progress, resolve, close — with domain invariants and audit trail.

- Built **SignalR** hub with Redis scale-out backplane; immediate + worker-driven push paths for sub-second collaborative UX.

- Exposed **Prometheus metrics** and OTLP hooks; dead-letter change queue; operations and audit APIs for tenant admins.

---

## Full-stack

- React 19 SPA with dark compact UI, **Tickets + Queues** pages, bucket-scoped SignalR, row-level push patch, assignee dropdown from tenant user list, workflow hints, remote-change flash, and **tenant admin console** (overview, users, audit, settings).

- Admin dashboard surfaces change-queue health (pending / dead-letter) wired to operations API and Prometheus metrics.

---

## Interview one-liner

> "I built LiveSync to demonstrate how I'd architect a multi-tenant support desk SaaS: isolated tenant databases, DDD with Queue and Ticket aggregates, CQRS with an outbox and dead-letter path for reliable real-time sync, split API/worker deployment, bucket-scoped SignalR groups, row-level client push updates, ticket workflow with domain invariants, Prometheus metrics, tenant lifecycle with audit — plus a tenant admin UI — with ADRs and NFRs documenting the trade-offs."

---

## GitHub About section (short)

```
Multi-tenant support desk SaaS (.NET 10 + React) — DB-per-tenant, DDD (Queues + Tickets),
CQRS, outbox + dead-letter, bucket-scoped SignalR, Prometheus/OTLP, tenant admin console.
C4, ADRs, NFRs. See docs/solution-architecture.md
```

---

## Skills to tag on GitHub / LinkedIn

`Solution architecture` · `Multi-tenancy` · `DDD` · `CQRS` · `Event-driven` · `SignalR` · `Redis` · `SQL Server` · `.NET` · `Docker` · `Prometheus` · `OpenTelemetry` · `ADR` · `SaaS` · `System design`

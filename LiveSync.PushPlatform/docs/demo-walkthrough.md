# LiveSync — Interactive demo walkthrough

Follow this guide to experience the features that matter most to technical reviewers: **multi-tenancy**, **RBAC**, **real-time sync**, **support desk workflow**, **tenant admin**, and **observability**.

Estimated time: **20 minutes**.

---

## Before you start

```bash
cd LiveSync.PushPlatform
docker compose up -d
cd LiveSync.API/client && npm install && npm run build
dotnet run --project LiveSync.API
# New terminal:
dotnet run --project LiveSync.Worker
```

Open **http://localhost:5252** (or http://localhost:5173 if using Vite dev mode).

> **Faster setup:** `.\scripts\dev.ps1` (Windows) or `./scripts/dev.sh` (Linux/macOS) — Docker, client build, API + Worker.

> **Problems?** See [troubleshooting.md](troubleshooting.md).

---

## Scenario 1 — Login and explore (2 min)

1. Sign in with the seeded admin:
   - Email: `admin@livesync.local`
   - Password: `Admin123!`
2. You land on **Tickets** — paginated list (newest first).
3. Check the header: `livesync` wordmark and `t/1` tenant reference.
4. On Tickets, confirm **signalr · live** status (green dot).
5. Open **Account** — note roles (`TenantAdmin`), organization name, and tenant status.
6. Open **Queues** — see the default **General** queue.

**What you're seeing:** JWT auth, tenant-scoped support desk data, SignalR subscription active, dark compact UI.

---

## Scenario 2 — Real-time sync, two users, same tenant (5 min)

This is the headline demo. Use **two browser contexts** so sessions don't overwrite each other.

### Setup

| Window | User | How to open |
|--------|------|-------------|
| Tab A | Admin (already logged in) | Normal browser window |
| Tab B | Second user | **Private/Incognito** window |

### Create the second user (Tab A — admin)

1. In Tab A, go to **Admin → Users**.
2. Invite a user, for example:
   - Username: `member1`
   - Email: `member1@livesync.local`
   - Password: `Member123!`
   - Display name: `Member One`

### Log in as member (Tab B)

1. Open incognito → http://localhost:5252/login
2. Sign in as `member1` / `Member123!`
3. Go to **Tickets** — same tenant, same ticket universe (IDs are per-tenant).
4. Confirm **signalr · live** on both tabs.

### The live sync test

1. **Tab A (admin):** Open a ticket (subject: `From Admin`).
2. **Tab B (member):** Row appears within ~1 second — no manual refresh (table row patch).
3. **Tab B:** Open another ticket (`From Member`).
4. **Tab A:** Row updates immediately.
5. **Tab B:** Select a ticket → add a comment.
6. **Tab A:** Comment appears in the detail panel (push refreshes selected ticket).

**What you're seeing:**

- API pushes `PushUpdate` to SignalR group `tenant:{id}:bucket:ticket` on every ticket mutation.
- Worker processes the change queue for Redis subscription cache consistency.
- Both users share tenant data but have different roles (RBAC).
- Rows changed by the *other* session show a red lightning flash icon at row end.

### Bucket isolation (Queues vs Tickets)

1. **Tab A:** Stay on **Tickets**; **Tab B:** Open **Queues** (same tenant).
2. Open or comment on a ticket in Tab A — **Queues tab does not update** (different bucket).
3. Create a queue in Tab B — **Tickets tab does not update**.

**What you're seeing:** bucket-scoped SignalR groups ([ADR 005](adr/005-multi-bucket-real-time-sync.md)).

### Record this for your README GIF

Use [ScreenToGif](https://www.screentogif.com/) or OBS:

1. Arrange windows side-by-side (admin + member).
2. Open a ticket or add a comment in each tab.
3. Export as `docs/assets/demo-realtime-sync.gif` (15–30 seconds, &lt; 10 MB).

---

## Scenario 3 — Ticket workflow and RBAC (3 min)

Still in Tab A (admin) and Tab B (member):

### Status workflow

Ticket status follows a strict lifecycle (not a free-form dropdown):

```
New → Assigned → In progress → Resolved → Closed
```

1. **Tab A (admin):** Select a **New** ticket → click **Assign** → pick a user from the tenant dropdown.
2. Status becomes **Assigned**. Click **Start progress** (available once assignee is set).
3. Click **Resolve**, then **Close**.

The detail panel shows a hint for the next step (e.g. *"Click Resolve when the issue is fixed."*).

### RBAC

| Action | Admin (Tab A) | Member (Tab B) |
|--------|---------------|----------------|
| Open ticket | ✅ | ✅ |
| Add comment | ✅ | ✅ |
| Start progress / Resolve / Close | ✅ | ✅ |
| Assign ticket | ✅ | ❌ button hidden |
| Delete ticket | ✅ | ❌ button hidden |
| Admin nav link | ✅ | ❌ not shown |
| Invite user | ✅ Admin → Users | ❌ forbidden |

Try assigning as member via API — `PUT /api/v1/tickets/{id}/assign` returns **403 Forbidden**.

---

## Scenario 4 — Tenant isolation (optional, 3 min)

1. Register a **new tenant** at `/register` (incognito, different email).
2. Note the new tenant id in the header (`t/2`, etc.).
3. Tickets and queues in tenant 1 are **not** visible in tenant 2.
4. Each tenant has its own **General** queue and independent ticket IDs.

**What you're seeing:** database-per-tenant isolation + JWT `tenant_id` claim.

---

## Scenario 5 — Tenant admin console (3 min)

As **TenantAdmin** in Tab A:

### Overview

1. Go to **Admin → Overview**.
2. See organization name, status, **queue pending**, and **dead letter** counts.
3. Counts update when the Worker is running.

### Audit log

1. Open or assign a ticket.
2. Go to **Admin → Audit**.
3. Confirm `open`, `assign`, or `comment` entries with user id, entity type `ticket`, and details.

### Suspend / reactivate

1. Go to **Admin → Settings**.
2. **Suspend** the tenant (confirm dialog).
3. Try loading **Tickets** — blocked (403).
4. Return to **Admin → Settings** and **Reactivate**.
5. Tickets work again.

**What you're seeing:** tenant lifecycle API, `TenantStatusMiddleware`, audit trail.

---

## Scenario 6 — API explorer (1 min)

Development only:

- Open **http://localhost:5252/scalar/v1**
- Try `POST /api/v1/auth/login` → copy token
- Call `GET /api/v1/tickets` with `Authorization: Bearer {token}`
- Call `GET /api/v1/auth/users` — list tenant members (for assignee picker)
- Try `GET /api/v1/operations/change-queue` (TenantAdmin)
- Try `GET /api/v1/audit` (TenantAdmin)

### Idempotency (optional)

```http
POST /api/v1/tickets
Authorization: Bearer {token}
Idempotency-Key: demo-key-001
Content-Type: application/json

{
  "queueId": 1,
  "subject": "Idempotent ticket",
  "description": "Test",
  "priority": 1,
  "reporterUserId": 1
}
```

Replay the same request with the same key — returns the **same ticket id**.

---

## Scenario 7 — Observability (optional, 3 min)

Start the observability stack:

```bash
cd observability
docker compose -f docker-compose.observability.yml --profile observability up -d
```

Ensure API and Worker are running. In `LiveSync.API/appsettings.json`, `Observability:Otlp:Endpoint` defaults to `http://localhost:4317`.

| Tool | URL | Notes |
|------|-----|-------|
| Prometheus | http://localhost:9090 | Query `livesync_change_queue_depth`; check **Targets** |
| Grafana | http://localhost:3000 | Login `admin` / `admin`; add Prometheus datasource `http://prometheus:9090` |
| Raw metrics | http://localhost:5252/metrics | API scrape endpoint |
| Worker metrics | http://localhost:5260/metrics | Worker scrape endpoint |

> **OTLP (`:4317`)** is gRPC for the collector — not a browser URL.

---

## Scenario 8 — Queues CRUD (2 min)

As any authenticated user:

1. Go to **Queues**.
2. Create a queue (e.g. `IT Support`) — appears at top of list (newest first).
3. Rename via dialog.

As **TenantAdmin**:

4. Try to **deactivate** a queue that has open tickets — **409 Conflict** (business rule).
5. Close all tickets in that queue, then deactivate succeeds.
6. **Delete** an empty or deactivated queue.

Member users can create and rename queues but not deactivate or delete.

---

## Troubleshooting

See **[troubleshooting.md](troubleshooting.md)** for the full table (SignalR offline, empty wwwroot, Docker SQL, JWT, etc.).

---

## What to tell an interviewer

> "LiveSync is a database-per-tenant support desk SaaS sample. The API handles auth and CQRS for queues and tickets with a real status machine in the domain; comments are entities inside the ticket aggregate. Domain events enqueue changes; a worker processes the outbox with dead-letter handling; bucket-scoped SignalR groups push updates so only relevant pages refresh; the SPA patches table rows instead of full refetch. Tenant admins assign agents from a tenant user list, manage queues, audit, queue health, and suspend/reactivate. Prometheus metrics and OTLP hooks show operability thinking — with ADRs and NFRs documenting trade-offs."

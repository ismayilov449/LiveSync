# LiveSync — Interactive demo walkthrough

Follow this guide to experience the features that matter most to technical reviewers: **multi-tenancy**, **RBAC**, **real-time sync**, **tenant admin**, and **observability**.

Estimated time: **15 minutes**.

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

---

## Scenario 1 — Login and explore (2 min)

1. Sign in with the seeded admin:
   - Email: `admin@livesync.local`
   - Password: `Admin123!`
2. Go to **Items** — paginated list (newest first).
3. Check the header: `livesync` wordmark and `t/1` tenant reference.
4. On Items, confirm **signalr · live** status (green dot).
5. Open **Account** — note roles (`TenantAdmin`), organization name, and tenant status.

**What you're seeing:** JWT auth, tenant-scoped data, SignalR subscription active, dark compact UI.

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
3. Go to **Items** — same tenant, same item universe (IDs are per-tenant).
4. Confirm **signalr · live** on both tabs.

### The live sync test

1. **Tab A (admin):** Create an item named `From Admin`.
2. **Tab B (member):** Should refresh within ~1 second — no manual refresh.
3. **Tab B:** Create an item named `From Member`.
4. **Tab A:** Should also update immediately.

**What you're seeing:**

- API pushes `PushUpdate` to SignalR group `tenant:{id}` on every item mutation.
- Worker processes the change queue for Redis subscription cache consistency.
- Both users share tenant data but have different roles (RBAC).

### Record this for your README GIF

Use [ScreenToGif](https://www.screentogif.com/) or OBS:

1. Arrange windows side-by-side (admin + member).
2. Create one item in each tab.
3. Export as `docs/assets/demo-realtime-sync.gif` (15–30 seconds, &lt; 10 MB).

---

## Scenario 3 — RBAC (2 min)

Still in Tab A (admin) and Tab B (member):

| Action | Admin (Tab A) | Member (Tab B) |
|--------|---------------|----------------|
| Create item | ✅ | ✅ |
| Rename item | ✅ | ✅ |
| Move / Deactivate / Delete | ✅ buttons visible | ❌ buttons hidden |
| Admin nav link | ✅ | ❌ not shown |
| Invite user | ✅ Admin → Users | ❌ forbidden |

Try deleting as member via API — `DELETE /api/v1/items/{id}` returns **403 Forbidden**.

---

## Scenario 4 — Tenant isolation (optional, 3 min)

1. Register a **new tenant** at `/register` (incognito, different email).
2. Note the new tenant id in the header (`t/2`, etc.).
3. Items in tenant 1 are **not** visible in tenant 2.
4. Parent item IDs from tenant 1 **do not work** in tenant 2 (each tenant has its own Root).

**What you're seeing:** database-per-tenant isolation + JWT `tenant_id` claim.

---

## Scenario 5 — Tenant admin console (3 min)

As **TenantAdmin** in Tab A:

### Overview

1. Go to **Admin → Overview**.
2. See organization name, status, **queue pending**, and **dead letter** counts.
3. Counts update when the Worker is running.

### Audit log

1. Create or rename an item.
2. Go to **Admin → Audit**.
3. Confirm a `create` or `update` entry with user id, entity, and details.

### Suspend / reactivate

1. Go to **Admin → Settings**.
2. **Suspend** the tenant (confirm dialog).
3. Try loading **Items** — blocked (403).
4. Return to **Admin → Settings** and **Reactivate**.
5. Items work again.

**What you're seeing:** tenant lifecycle API, `TenantStatusMiddleware`, audit trail.

---

## Scenario 6 — API explorer (1 min)

Development only:

- Open **http://localhost:5252/scalar/v1**
- Try `POST /api/v1/auth/login` → copy token
- Call `GET /api/v1/items` with `Authorization: Bearer {token}`
- Try `GET /api/v1/operations/change-queue` (TenantAdmin)
- Try `GET /api/v1/audit` (TenantAdmin)

### Idempotency (optional)

```http
POST /api/v1/items
Authorization: Bearer {token}
Idempotency-Key: demo-key-001
Content-Type: application/json

{ "parentId": 1, "name": "Idempotent item" }
```

Replay the same request with the same key — returns the **same item id**.

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

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| SignalR shows **offline** | Ensure API is running; hard-refresh (Ctrl+Shift+R) |
| No live updates | Start **Worker**; ensure Redis is up: `docker compose up -d redis` |
| Empty Items page after clone | Run `npm run build` in `LiveSync.API/client` |
| Login fails | SQL Server container running? `docker compose ps` |
| Port 5252 in use | Stop old API process or change port in `launchSettings.json` |
| Queue stats show `—` | Worker not running or tenant suspended |
| Prometheus targets down | On Windows/Mac use `host.docker.internal`; API/Worker must be on host ports 5252/5260 |
| Admin nav missing | User must have `TenantAdmin` role |

---

## What to tell an interviewer

> "LiveSync is a database-per-tenant SaaS sample. The API handles auth and CQRS CRUD; domain events enqueue changes; a worker processes the outbox with dead-letter handling and keeps Redis subscription caches warm; SignalR tenant groups push updates so every user in the org sees changes instantly. Tenant admins get a console for users, audit, queue health, and suspend/reactivate. Prometheus metrics and OTLP hooks show operability thinking — with ADRs and NFRs documenting trade-offs."

# LiveSync — Interactive demo walkthrough

Follow this guide to experience the features that matter most to technical reviewers: **multi-tenancy**, **RBAC**, and **real-time sync across users**.

Estimated time: **10 minutes**.

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
2. Go to **Items** — you should see a paginated list (newest first).
3. Check the header: **Tenant 1** and **SignalR: Live** badge.
4. Open **Profile** — note roles (`TenantAdmin`) and tenant ID.

**What you're seeing:** JWT auth, tenant-scoped data, SignalR subscription active.

---

## Scenario 2 — Real-time sync, two users, same tenant (5 min)

This is the headline demo. Use **two browser contexts** so sessions don't overwrite each other.

### Setup

| Window | User | How to open |
|--------|------|-------------|
| Tab A | Admin (already logged in) | Normal browser window |
| Tab B | Second user | **Private/Incognito** window |

### Create the second user (Tab A — admin)

1. In Tab A, go to **Profile** → scroll to **Invite user** (or nav **Invite**).
2. Create a user, for example:
   - Username: `member1`
   - Email: `member1@livesync.local`
   - Password: `Member123!`
   - Display name: `Member One`

### Log in as member (Tab B)

1. Open incognito → http://localhost:5252/login
2. Sign in as `member1` / `Member123!`
3. Go to **Items** — same tenant, same item universe (IDs are per-tenant).
4. Confirm **SignalR: Live** on both tabs.

### The live sync test

1. **Tab A (admin):** Create an item named `From Admin`.
2. **Tab B (member):** Should refresh within ~1 second — no manual refresh.
3. **Tab B:** Create an item named `From Member`.
4. **Tab A:** Should also update immediately.

**What you're seeing:**

- API pushes `PushUpdate` to SignalR group `tenant:{id}` on every item mutation.
- Worker also processes the change queue for Redis subscription cache consistency.
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
| Invite user | ✅ Profile → Invite | ❌ forbidden |

Try deleting as member via API — `DELETE /api/v1/items/{id}` returns **403 Forbidden**.

---

## Scenario 4 — Tenant isolation (optional, 3 min)

1. Register a **new tenant** at `/register` (incognito, different email).
2. Note the new **Tenant ID** in the header.
3. Items in tenant 1 are **not** visible in tenant 2.
4. Parent item IDs from tenant 1 **do not work** in tenant 2 (each tenant has its own Root).

**What you're seeing:** database-per-tenant isolation + JWT `tenant_id` claim.

---

## Scenario 5 — API explorer (1 min)

Development only:

- Open **http://localhost:5252/scalar/v1**
- Try `POST /api/v1/auth/login` → copy token
- Call `GET /api/v1/items` with `Authorization: Bearer {token}`

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| SignalR shows **Offline** | Ensure API is running; hard-refresh (Ctrl+Shift+R) |
| No live updates | Start **Worker**; ensure Redis is up: `docker compose up -d redis` |
| Empty Items page after clone | Run `npm run build` in `LiveSync.API/client` |
| Login fails | SQL Server container running? `docker compose ps` |
| Port 5252 in use | Stop old API process or change port in `launchSettings.json` |

---

## What to tell an interviewer

> "LiveSync is a database-per-tenant SaaS sample. The API handles auth and CRUD with CQRS; domain events enqueue changes; a worker processes the outbox and keeps Redis subscription caches warm; SignalR pushes tenant-wide updates so every connected user in the same org sees changes instantly. I can demo two users in one tenant updating the same list in real time."

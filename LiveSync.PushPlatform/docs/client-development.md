# Client development guide

The React SPA lives in `LiveSync.API/client/` and builds into `LiveSync.API/wwwroot/` (gitignored).

## Prerequisites

- Node.js 20+
- API running on http://localhost:5252 (or use Vite proxy)

## Commands

| Command | Purpose |
|---------|---------|
| `npm ci` | Install dependencies |
| `npm run dev` | Vite dev server http://localhost:5173 (proxies API + SignalR) |
| `npm run build` | Typecheck + production build → `../wwwroot` |
| `npm test` | Vitest unit tests (`pushListPatch.test.ts`) |
| `npm run lint` | ESLint |

## Project layout

```
client/src/
├── api/           # Typed REST wrappers (tickets, queues, auth, audit, ops)
├── auth/          # AuthContext, ProtectedRoute, AdminRoute
├── components/    # Layout, Modal (Confirm, Select), RemotePushFlash
├── hooks/         # useDomainPush, useSupportDeskPush, useProfile, useRemotePushHighlights
├── pages/         # TicketsPage, QueuesPage, admin/*, auth
├── types/         # Shared TypeScript interfaces
└── utils/         # pushListPatch (row-level push updates)
```

## Vite proxy

`vite.config.ts` proxies `/api`, `/hubs`, and `/health` to port `5252`. Use `npm run dev` for fast HMR without rebuilding `wwwroot`.

## Auth flow

1. Login/register → `AuthSession` stored in `localStorage`
2. `useAccessToken()` provides Bearer token for API calls
3. `ProtectedRoute` / `AdminRoute` guard routes in `App.tsx`
4. SignalR connects with `?access_token=` query param

## Routes

| Path | Component | Notes |
|------|-----------|-------|
| `/` | redirect | → `/tickets` |
| `/tickets` | `TicketsPage` | List + detail panel + workflow |
| `/queues` | `QueuesPage` | Queue CRUD |
| `/admin/*` | Admin layout | TenantAdmin only |
| `/profile` | Profile page | All authenticated users |

## Real-time sync

### `useDomainPush`

Generic hook (`hooks/useDomainPush.ts`):

- Connects to `/hubs/push`
- Calls `FindAndSubscribe` with bucket + tenant filter
- Invokes `onPush(notification)` when `entity.bucket` matches

### Bucket-specific hooks (`hooks/useSupportDeskPush.ts`)

- `useTicketsPush` → bucket `Ticket`, filter `ticket.TenantId == N`
- `useQueuesPush` → bucket `Queue`, filter `queue.TenantId == N`

### Push payload

```typescript
interface ChangeNotificationDto {
  operation: number; // 1=Upsert, 2=Delete
  entity: { id: string; bucket: string }; // e.g. "ticket-42"
  change?: unknown;
}
```

### Row patch (Option B)

`utils/pushListPatch.ts`:

- **Delete** — remove row, decrement `totalCount`
- **Upsert** — `GET /tickets/{id}` or `GET /queues/{id}`, patch row in place
- Page 1 inserts new rows sorted newest-first; other pages bump count only
- Vitest tests cover ticket/queue patch helpers

### Remote change indicator

`useRemotePushHighlights` + `RemotePushFlash` show a red lightning icon on rows updated by **another session**. Own actions are suppressed for ~10s. Cleared on Refresh or navigation.

## Tickets page UX

- **Open ticket** form — queue, subject, description, priority
- **Detail panel** — status pill, workflow buttons, comments
- **Assign** — `SelectDialog` populated from `GET /api/v1/auth/users`
- **Workflow hints** — muted text under status explaining next step
- **User labels** — reporter, assignee, comment authors show display names

## Adding a new page

1. Create `src/pages/MyPage.tsx`
2. Add route in `App.tsx`
3. Add nav link in `Layout.tsx` if needed
4. Add API methods in `src/api/index.ts` + types in `src/types/index.ts`
5. If real-time: add bucket on server, then `useDomainPush(..., 'MyBucket', ...)` or wrapper in `useSupportDeskPush.ts`

See [extending-the-platform.md](extending-the-platform.md) for full-stack steps.

## Styling

Global styles in `src/index.css`. Dark compact theme with `--surface`, `--accent`, status pills, and table sync hints.

## Debugging SignalR

- Tickets/Queues header shows **signalr · live** when connected
- Browser DevTools → Network → WS → `/hubs/push`
- Ensure Worker is running for queue consistency (immediate API push still works without Worker)

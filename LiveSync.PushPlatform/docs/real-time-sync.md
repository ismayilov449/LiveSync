# Real-time sync pipeline

How LiveSync keeps multiple browser tabs (and users) in sync within the same tenant.

## Two notification paths (by design)

| Path | When | Purpose |
|------|------|---------|
| **API immediate push** | Right after item save in API | Fast UI refresh for all users in tenant |
| **Worker queue processing** | Polls `ChangeQueue` every ~1s | Redis topic cache, filtered subscriptions, consistency |

Both send `PushUpdate` to SignalR group `tenant:{tenantId}`.

## Sequence — create item

```mermaid
sequenceDiagram
    autonumber
    participant TabA as Browser Tab A
    participant TabB as Browser Tab B
    participant API as LiveSync API
    participant DB as Tenant DB
    participant Q as ChangeQueue
    participant W as Worker
    participant Redis as Redis
    participant Hub as SignalR Hub

    TabA->>API: POST /api/v1/items
    API->>DB: INSERT Item + SaveChanges
    API->>API: ItemCreatedDomainEvent
  Note over API,Hub: Immediate path
    API->>Hub: NotifyTenantAsync(tenant:1)
    Hub->>TabA: PushUpdate
    Hub->>TabB: PushUpdate
    API->>Q: Enqueue change envelope
    TabA->>API: GET /api/v1/items (refresh)
    TabB->>API: GET /api/v1/items (refresh)

  Note over W,Redis: Background path (~1s)
    W->>Q: Claim batch
    W->>DB: Load item DTO
    W->>Redis: Upsert topic cache
    W->>Hub: NotifyTenantAsync(tenant:1)
    Hub->>TabA: PushUpdate (debounced client-side)
    Hub->>TabB: PushUpdate
```

## Client behavior

1. On Items page load → connect to `/hubs/push?access_token=...`
2. Hub adds connection to group `tenant:{tenantId}`
3. `FindAndSubscribe` registers Redis subscription (for filtered cache snapshots)
4. On `PushUpdate` → debounced refresh of page 1 (newest items first)
5. Stale HTTP responses are ignored if a newer refresh is in flight

## SignalR groups vs connection IDs

Early versions targeted individual `connectionId` values stored in Redis. Reconnects (background tabs, network blips) could leave stale IDs. **Tenant groups** ensure every live connection for a tenant receives pushes regardless of subscription record state.

## Redis responsibilities

| Key pattern | Role |
|-------------|------|
| `{tenantId}:livesync:subs:*` | Subscription registry |
| `{tenantId}:livesync:topics:bucket:*` | Active filter topics |
| Topic hash keys | Cached DTO snapshots per filter |
| SignalR backplane | Cross-process hub messaging (API ↔ Worker) |

Shared channel prefix: `LiveSync` (see `LiveSyncSignalR.RedisChannelPrefix`).

## Failure modes

| Symptom | Likely cause |
|---------|----------------|
| Creator sees item, others don't | SignalR offline; check badge |
| One user always behind | Fixed: stale fetch race + worker-only push |
| Nothing live at all | Redis down; Worker not running |
| Only works for one user | Tab not in tenant group; re-login |

See [demo-walkthrough.md](demo-walkthrough.md) for hands-on verification.

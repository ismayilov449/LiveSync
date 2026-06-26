# ADR 004: SignalR tenant groups for broadcast

## Status

**Amended** by [ADR 005](005-multi-bucket-real-time-sync.md) — delivery now uses **bucket-scoped** groups `tenant:{tenantId}:bucket:{ticket|queue}`.

## Context

Real-time updates must reach **all authenticated users in a tenant** who care about a given data type, not only the user who made the change. An earlier approach targeted individual `connectionId` values stored in Redis subscriptions. Browser reconnects and background tabs produced stale connection IDs — some users missed pushes or saw delayed updates.

## Decision (original)

1. On SignalR connect / `FindAndSubscribe`, add each connection to a **tenant-scoped group** (initially `tenant:{tenantId}`)
2. On entity changes, call `IHubContext.Clients.Group(...).PushUpdate(...)`
3. Keep Redis subscription registry for **filtered cache snapshots** (DynamicExpresso filters), not as the sole delivery mechanism

Worker and API share the same Redis SignalR backplane channel prefix (`LiveSync`).

## Amendment (ADR 005)

Tenant-wide groups caused unrelated pages (e.g. Queues) to refresh on Ticket changes. Groups are now **per bucket**:

- `tenant:{tenantId}:bucket:ticket`
- `tenant:{tenantId}:bucket:queue`

Connections join the bucket group matching their `FindAndSubscribe` bucket.

## Consequences

### Positive

- All live connections subscribed to a bucket receive pushes after reconnect
- Worker can notify clients attached to any API instance (scale-out ready)
- Unrelated UI pages no longer react to other buckets' pushes

### Negative

- Multiple groups when a client subscribes to several buckets (acceptable today)
- Group membership must be cleaned on disconnect

### Mitigations

- `OnDisconnectedAsync` removes connection from groups
- Client row-patch updates instead of full-page refetch; remote-change flash for cross-session visibility

## Related

- [real-time-sync.md](../real-time-sync.md)
- [ADR 005](005-multi-bucket-real-time-sync.md)

# ADR 004: SignalR tenant groups for broadcast

## Status

Accepted

## Context

Real-time updates must reach **all authenticated users in a tenant**, not only the user who made the change. An earlier approach targeted individual `connectionId` values stored in Redis subscriptions. Browser reconnects and background tabs produced stale connection IDs — some users missed pushes or saw delayed updates.

## Decision

1. On SignalR connect / `FindAndSubscribe`, add each connection to group `tenant:{tenantId}`
2. On item changes, call `IHubContext.Clients.Group("tenant:{id}").PushUpdate(...)`
3. Keep Redis subscription registry for **filtered cache snapshots** (DynamicExpresso filters), not as the sole delivery mechanism

Worker and API share the same Redis SignalR backplane channel prefix (`LiveSync`).

## Consequences

### Positive

- All live connections in a tenant receive pushes after reconnect
- Worker can notify clients attached to any API instance (scale-out ready)

### Negative

- Tenant-wide push ignores per-user filter nuance for list refresh (acceptable for current Items UI)
- Group membership must be cleaned on disconnect

### Mitigations

- `OnDisconnectedAsync` removes connection from tenant group
- Client debounces rapid pushes and ignores stale HTTP responses

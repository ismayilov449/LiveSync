# ADR 005: Multi-bucket real-time sync (Tickets + Queues)

## Status

Accepted (amends [ADR 004](004-signalr-tenant-groups.md))

## Context

LiveSync needed real-time sync for multiple domain aggregates without spamming unrelated UI pages. An earlier tenant-wide SignalR group (`tenant:{tenantId}`) caused every connected tab to receive every push.

The platform evolved through two aggregate pairs:

1. **Original:** Items + Categories (`TopicBucket.Item`, `TopicBucket.Category`)
2. **Current:** Tickets + Queues (`TopicBucket.Ticket`, `TopicBucket.Queue`) per [ADR 006](006-support-desk-aggregates.md)

The multi-bucket **mechanism** is unchanged; only bucket names and handlers were renamed.

## Decision

1. `TopicBucket` enum: `Ticket = 1`, `Queue = 2`
2. **Bucket-scoped SignalR groups**: `tenant:{tenantId}:bucket:{ticket|queue}`
3. On hub connect / `FindAndSubscribe`, add connection to the bucket group for the subscribed bucket
4. `IRealTimeNotifier.NotifyBucketAsync(tenantId, bucket, notification)` targets only that group
5. Client hooks (`useDomainPush`, `useTicketsPush`, `useQueuesPush`) ignore pushes where `entity.bucket` does not match
6. SPA uses **row-level patch** on push (fetch single entity + patch table) instead of full list refetch
7. **Remote push flash** — visual indicator when another session changed a row

## Consequences

### Positive

- Tickets page ignores Queue pushes and vice versa
- Pattern scales to additional aggregates (see [extending-the-platform.md](../extending-the-platform.md))
- Less network and UI churn per tab

### Negative

- More groups per connection when subscribing to multiple buckets (acceptable for current UI)
- ADR 004 tenant-wide model is superseded for delivery (groups are now per-bucket)

### Mitigations

- Integration test `BucketScopedPushIntegrationTests` verifies bucket isolation
- Document extension path in [extending-the-platform.md](../extending-the-platform.md)

## Related

- [real-time-sync.md](../real-time-sync.md) — dual push paths, client patch behavior
- [client-development.md](../client-development.md) — `useDomainPush` and push hooks
- [ADR 006](006-support-desk-aggregates.md) — current domain aggregates

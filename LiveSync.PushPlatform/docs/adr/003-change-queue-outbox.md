# ADR 003: SQL change queue (transactional outbox)

## Status

Accepted

## Context

Ticket and queue mutations must reliably trigger real-time cache updates and optional filtered subscription logic. Publishing directly to Redis/SignalR from the API request thread couples availability and makes retries harder.

## Decision

Use a **ChangeQueue** table in each tenant database as an outbox:

1. Domain events (`TicketOpened`, `TicketAssigned`, `QueueCreated`, etc.) enqueue a row in the same tenant DB with status **Pending**
2. **LiveSync.Worker** polls, claims batches with row locks, processes via bucket handlers (`TicketChangeHandler`, `QueueChangeHandler`)
3. Successful entries are deleted; failures increment retry count
4. After `ChangeDetection:MaxRetries` (default 5), status becomes **DeadLetter** for operator inspection

Queue statistics are exposed via `GET /api/v1/operations/change-queue` and Prometheus gauges `livesync_change_queue_depth` / `livesync_change_queue_dead_letter_depth`.

## Consequences

### Positive

- Decouples user request latency from downstream sync work
- Survives transient Redis/SignalR failures (retries)
- Per-tenant queue naturally partitions load

### Negative

- Eventual consistency (poll interval, typically ~1s)
- Additional table and worker complexity

### Mitigations

- **API immediate SignalR push** on domain events for sub-second UX
- Worker path keeps Redis topic cache consistent for filtered subscriptions

## Historical note

Originally implemented for Item/Category mutations. [ADR 006](006-support-desk-aggregates.md) replaced those aggregates with Tickets and Queues; the outbox pattern is unchanged.

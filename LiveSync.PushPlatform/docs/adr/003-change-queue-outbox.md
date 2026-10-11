# ADR 003: SQL change queue (transactional outbox)

## Status

Accepted

## Context

Ticket and queue mutations must reliably trigger real-time cache updates and optional filtered subscription logic. Publishing directly to Redis/SignalR from the API request thread couples availability and makes retries harder.

## Decision

Use a **ChangeQueue** table in each tenant database as an outbox:

1. Domain events (`TicketOpened`, `TicketAssigned`, `QueueCreated`, etc.) enqueue a row in the same tenant DB with status **Pending**, **in the same transaction** as the aggregate change (see *Transaction boundary* below)
2. **LiveSync.Worker** polls, claims batches with row locks, processes via bucket handlers (`TicketChangeHandler`, `QueueChangeHandler`)
3. Successful entries are deleted; failures increment retry count
4. After `ChangeDetection:MaxRetries` (default 5), status becomes **DeadLetter** for operator inspection

### Transaction boundary

Commands that change aggregates implement `ITransactionalCommand`. `TransactionBehavior` (MediatR pipeline) runs the handler through `ITransactionRunner` (`EfTransactionRunner`), which opens a tenant DB transaction inside EF Core's execution strategy:

1. Handler saves the aggregate (identity keys are generated)
2. Domain events are dispatched; `Enqueue*DomainEventHandler` writes the `ChangeQueue` row
3. Transaction commits — the business row and its outbox row exist together, or neither does
4. On a transient SQL error the whole command is re-run against a cleared change tracker

The API's immediate SignalR push is buffered in `PostCommitNotifications` and sent only **after** commit, so clients are never told about a rolled-back change. A failed immediate push is logged and does not fail the request — the outbox row guarantees the Worker delivers it.

`TransactionalOutboxIntegrationTests` verifies that a failing outbox write rolls back the ticket.

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

Before the transaction boundary was added, the outbox row was saved in a separate `SaveChanges` after the business commit, so a crash between the two could lose the change notification. `TransactionBehavior` closed that gap.

Originally implemented for Item/Category mutations. [ADR 006](006-support-desk-aggregates.md) replaced those aggregates with Tickets and Queues; the outbox pattern is unchanged.

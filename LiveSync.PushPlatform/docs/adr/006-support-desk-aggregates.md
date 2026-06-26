# ADR 006: Support Desk aggregates (Queue + Ticket)

## Status

Accepted

## Context

Items and Categories were scaffolding to validate the platform (tenancy, CQRS, outbox, SignalR). For a portfolio DDD story we need a **cohesive bounded context** with real invariants.

## Decision

Replace Items/Categories with **Support Desk**:

### Queue (aggregate)
- Flat list of work streams per tenant (e.g. "General", "IT")
- `Create`, `Rename`, `Deactivate`, `Delete`
- **Rule:** cannot deactivate a queue with open tickets (`Status != Closed`)

### Ticket (aggregate root)
- Belongs to one queue (reference by `QueueId`)
- **Status machine:** `New → Assigned → InProgress → Resolved → Closed`
- **TicketComment** is an **entity inside** the ticket aggregate — no standalone Comments API
- Methods: `Open`, `Assign`, `AddComment`, `StartProgress`, `Resolve`, `Close`
- Invariants enforced in domain (e.g. no comments on closed tickets; resolve only from in-progress)

### Cross-aggregate
- `ITicketQueueValidator` (application/domain service) ensures queue exists and is active when opening tickets

### Bootstrap
- New tenants get a default **General** queue (replaces root item)

## Consequences

### Positive
- Clear ubiquitous language for demos and interviews
- Rich domain behavior beyond CRUD
- Comments demonstrate entity-vs-aggregate boundary

### Negative
- Breaking schema change (migration drops Items/Categories tables)
- Client and docs fully renamed

## Related

- [extending-the-platform.md](../extending-the-platform.md)
- [ADR 005](005-multi-bucket-real-time-sync.md) — `TopicBucket.Ticket`, `TopicBucket.Queue`

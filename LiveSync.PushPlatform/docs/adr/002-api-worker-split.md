# ADR 002: API and Worker process split

## Status

Accepted

## Context

LiveSync must serve HTTP/SignalR to browsers while continuously processing change queues and subscription expiry. These workloads have different scaling profiles and failure modes.

## Decision

Split into two deployable processes:

| Process | Responsibility |
|---------|----------------|
| **LiveSync.API** | REST, auth, SPA, SignalR hub, immediate tenant push on mutations, audit/lifecycle APIs, `/metrics` |
| **LiveSync.Worker** | Poll `ChangeQueue`, dead-letter handling, update Redis caches, subscription TTL cleanup, `/metrics` |

Both share SQL Server and Redis. SignalR uses the Redis backplane so the Worker can publish hub messages consumed by clients connected to the API.

## Consequences

### Positive

- API scales on request traffic; workers scale on queue depth
- Long-running poll loops do not block HTTP threads
- Worker can be restarted without dropping user-facing API (degraded live cache until back)

### Negative

- Two processes to deploy and monitor
- Requires Redis backplane for cross-process SignalR

### Mitigations

- Docker Compose full profile; shared health check patterns
- API immediate push reduces user-visible dependency on worker latency

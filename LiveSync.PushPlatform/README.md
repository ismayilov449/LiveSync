# LiveSync.PushPlatform

The .NET solution behind **LiveSync**, a multi-tenant support desk with database-per-tenant isolation, a transactional outbox and bucket-scoped real-time sync over SignalR.

**👉 The full project overview, architecture, demos and quick start live in the [repository README](../README.md).**

## In this folder

| Path | Contents |
|---|---|
| `LiveSync.Domain/` | Aggregates (`Ticket`, `Queue`), domain events, invariants |
| `LiveSync.Application/` | CQRS handlers, pipeline behaviors (`TransactionBehavior`), real-time ports, metrics |
| `LiveSync.Infrastructure/` | EF Core, tenancy, outbox store, Redis, SignalR, hosted services |
| `LiveSync.API/` | REST API, middleware, SignalR hub, React SPA (`client/`) |
| `LiveSync.Worker/` | Outbox processor, queue metrics, subscription expiry |
| `LiveSync.Tests/` · `LiveSync.IntegrationTests/` | Unit tests · Testcontainers integration tests |
| [`docs/`](docs/) | [Solution architecture](docs/solution-architecture.md), [ADRs](docs/adr/), [walkthrough](docs/demo-walkthrough.md), [glossary](docs/glossary.md) |
| `observability/` | Optional Prometheus, Grafana and OTLP collector stack |
| `scripts/` | `dev.ps1` / `dev.sh` quick start, `capture-demo-assets.py` |

## Common commands

```bash
docker compose up -d                       # SQL Server + Redis
dotnet run --project LiveSync.API          # http://localhost:5252
dotnet run --project LiveSync.Worker       # outbox processor, metrics on :5260
dotnet test LiveSync.Tests                 # unit tests
dotnet test LiveSync.IntegrationTests      # integration tests (Docker required)
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines and [docs/troubleshooting.md](docs/troubleshooting.md) for setup issues.

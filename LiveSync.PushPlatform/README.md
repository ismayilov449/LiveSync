# LiveSync — Multi-Tenant Real-Time Sync Platform

[![CI](https://github.com/ismayilov449/LiveSync/actions/workflows/ci.yml/badge.svg)](https://github.com/ismayilov449/LiveSync/actions/workflows/ci.yml)

Portfolio project demonstrating **database-per-tenant SaaS architecture** with CQRS, outbox-style change detection, Redis-backed subscriptions, and SignalR live updates.

## Features

- **Multi-tenancy** — control plane database + isolated tenant databases
- **CQRS** — MediatR commands/queries, FluentValidation pipeline, domain events
- **Real-time sync** — change queue → worker → Redis cache → SignalR push
- **Auth** — ASP.NET Identity, JWT, tenant-scoped RBAC (`TenantAdmin` / `TenantUser`)
- **RBAC** — `TenantAdmin` can invite users and delete/move/deactivate items; all users can read/create/rename
- **API** — versioned REST (`/api/v1/...`), OpenAPI/Scalar, ProblemDetails errors
- **Frontend** — React SPA with live item updates, pagination, user invite flow
- **Observability** — Serilog structured logging, OpenTelemetry traces, correlation IDs

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
│  React SPA  │────▶│  LiveSync    │────▶│ Control Plane   │
│  + SignalR  │     │  API         │     │ (SQL Server)    │
└─────────────┘     └──────┬───────┘     └─────────────────┘
                           │
                    ┌──────▼───────┐     ┌─────────────────┐
                    │ Change Queue │────▶│ LiveSync Worker │
                    │ (tenant DB)  │     └────────┬────────┘
                    └──────────────┘              │
                           │               ┌───────▼────────┐
                    ┌──────▼───────┐       │ Redis + SignalR│
                    │ Tenant DBs   │       └────────────────┘
                    │ (per tenant) │
                    └──────────────┘
```

See [docs/architecture.md](docs/architecture.md) and [docs/tenancy.md](docs/tenancy.md) for details.

## Tech stack

| Layer | Technologies |
|-------|--------------|
| Backend | .NET 10, ASP.NET Core, EF Core, MediatR, FluentValidation |
| Real-time | SignalR, Redis, background worker |
| Auth | ASP.NET Identity, JWT Bearer |
| Frontend | React 19, TypeScript, Vite, SignalR client |
| Data | SQL Server (control plane + tenant DBs), Redis |
| Testing | xUnit, Moq, Testcontainers, WebApplicationFactory |
| Ops | Docker Compose, GitHub Actions CI, Serilog, OpenTelemetry |

## Quick start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (SQL Server + Redis)

### 1. Start infrastructure

```bash
cd LiveSync.PushPlatform
docker compose up -d
```

### 2. Run the API

```bash
cd LiveSync.API/client && npm run build   # required — wwwroot/ is not committed
dotnet run --project LiveSync.API
```

API: http://localhost:5252  
OpenAPI (dev): http://localhost:5252/scalar/v1

### 3. Run the worker (required for live push)

```bash
dotnet run --project LiveSync.Worker
```

### 4. Frontend (optional — dev mode)

```bash
cd LiveSync.API/client
npm install
npm run dev
```

Vite dev server: http://localhost:5173 (proxies API/SignalR)

Or build into the API:

```bash
npm run build
```

### Demo credentials (Development seed)

| Field | Value |
|-------|-------|
| Email | `admin@livesync.local` |
| Password | `Admin123!` |
| Tenant | `1` (Default Tenant) |

## Docker — full stack

Run API + Worker + SQL Server + Redis:

```bash
docker compose --profile full up --build
```

API available at http://localhost:5252

## API overview

| Endpoint | Description |
|----------|-------------|
| `POST /api/v1/auth/register` | Create new tenant + admin user |
| `POST /api/v1/auth/login` | Login, receive JWT |
| `POST /api/v1/auth/users` | Invite user to your tenant (TenantAdmin) |
| `GET /api/v1/auth/me` | Current user profile + roles |
| `GET /api/v1/items` | Paginated items (newest first). Optional `parentId`, `page`, `pageSize` |
| `POST /api/v1/items` | Create item (any authenticated user) |
| `DELETE /api/v1/items/{id}` | Delete item (**TenantAdmin**) |
| `PUT /api/v1/items/{id}/parent` | Move item (**TenantAdmin**) |
| `POST /api/v1/items/{id}/deactivate` | Deactivate item (**TenantAdmin**) |
| `/hubs/push` | SignalR hub (JWT via `?access_token=`) |
| `/health`, `/health/ready`, `/health/live` | Health checks (API + Worker) |

Legacy unversioned aliases also work: `/api/items`, `/api/auth`.

> **Note:** `POST /api/v1/auth/register` creates a **new tenant**. To add users to an existing tenant, use `POST /api/v1/auth/users` as a tenant admin, or `POST /api/v1/auth/dev/users` in Development only.

## Project structure

```
LiveSync.PushPlatform/
├── LiveSync.Domain/           # Entities, interfaces, domain events
├── LiveSync.Application/      # CQRS handlers, real-time sync logic
├── LiveSync.Infrastructure/   # EF Core, Redis, SignalR, tenancy
├── LiveSync.API/              # REST API, auth, React SPA (client/)
├── LiveSync.Worker/           # Change detection + subscription expiry
├── LiveSync.Tests/            # Unit tests
├── LiveSync.IntegrationTests/ # Testcontainers integration tests
└── docs/                      # Architecture notes and ADRs
```

## Testing

```bash
# Unit tests
dotnet test LiveSync.Tests

# Integration tests (requires Docker)
dotnet test LiveSync.IntegrationTests
```

## Configuration

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:ControlPlane` | Central metadata database |
| `ConnectionStrings:Redis` | Redis for subscriptions/cache |
| `Tenancy:ConnectionTemplate` | Per-tenant DB connection template |
| `Auth:Jwt:SecretKey` | JWT signing key (use User Secrets in dev) |

Copy `appsettings.Development.json` patterns for local development. **Never commit production secrets.**

## Design decisions

- [ADR 001: Database per tenant](docs/adr/001-database-per-tenant.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT — see [LICENSE](LICENSE).

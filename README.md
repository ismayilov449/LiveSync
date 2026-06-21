# LiveSync Push Platform

Real-time filtered entity sync using SignalR, Redis, and a SQL-backed change queue.

## Architecture

| Process | Responsibility |
|---------|----------------|
| **LiveSync.API** | REST mutations, SignalR hub (`/hubs/push`), health at `/health` |
| **LiveSync.Worker** | Change queue polling, push delivery, subscription expiry |

Both processes share SQL Server and Redis. SignalR uses the Redis backplane so the worker can push to clients connected to the API.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for SQL Server + Redis)

## Quick start

```bash
# 1. Start infrastructure
docker compose up -d

# 2. Apply migrations (Development auto-migrates on API startup)
dotnet ef database update --project LiveSync.Infrastructure --startup-project LiveSync.API

# 3. Run API (http://localhost:5252)
dotnet run --project LiveSync.API

# 4. Run Worker (health at http://localhost:5260/health)
dotnet run --project LiveSync.Worker

# 5. Open test client
# http://localhost:5252/test-client.html
```

## Configuration

### API (`LiveSync.API/appsettings.json`)

- `ChangeDetection:Enabled` — **false** (worker handles queue processing)
- `Auth:ApiKey` — optional; when set, requires `X-Api-Key` header
- `Auth:RequireTenantHeaders` — when true (non-dev), requires `X-Tenant-Id` and `X-User-Id`

### Worker (`LiveSync.Worker/appsettings.json`)

- `ChangeDetection:Enabled` — **true**
- Runs subscription expiry and change detection hosted services

## SignalR subscription

Connect to `/hubs/push?tenantId=1&userId=1` and invoke:

```js
connection.invoke("FindAndSubscribe", {
  bucket: "Item",
  filter: "item.ParentId == 1"
});
```

Filters use [DynamicExpresso](https://github.com/dynamicexpresso/DynamicExpresso) syntax against the bucket DTO (`item` for the Item bucket).

## CQRS layout

Application CQRS lives under `LiveSync.Application/CQRS/`, split by domain:

```
CQRS/
  Items/
    Commands/      CreateItemCommand, CreateItemCommandHandler, ...
    Queries/       GetItemByIdQuery, ListItemsQuery, ...
    Models/        ItemDto
    Validators/    CreateItemCommandValidator, ...
    Services/      ItemHierarchyValidator (domain rules used by commands)
  RealTimeSync/
    Commands/      ProcessPendingChangesCommand, ...
```

| Layer | Role |
|-------|------|
| `ICommand` / `ICommand<T>` | Write operations |
| `IQuery<T>` | Read operations |
| `ICommandHandler<,>` / `IQueryHandler<,>` | Handlers |
| `CQRS/{Domain}/Models/` | Query/read DTOs |
| `LiveSync.API/Contracts/` | HTTP request bodies |
| `LiveSync.API/Mapping/` | Maps API contracts → commands |

Controllers bind HTTP contracts, map to commands/queries, and call `ISender.Send(...)`.

## REST API

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/items` | List items (`?parentId=` optional) |
| GET | `/api/items/{id}` | Get item by id |
| POST | `/api/items` | Create item |
| PUT | `/api/items/{id}` | Rename item |
| DELETE | `/api/items/{id}` | Delete item |
| POST | `/api/items/{id}/deactivate` | Deactivate item |
| PUT | `/api/items/{id}/parent` | Move item |

Pass tenant/user via headers: `X-Tenant-Id`, `X-User-Id`.

## Adding a new bucket

1. Add enum value to `TopicBucket`
2. Implement `IBucketModule` (fetch + deserialize DTO)
3. Implement `IChangeHandler`
4. Register both in `DependencyInjection`

## Development

- OpenAPI: `/openapi/v1.json`
- Scalar UI (dev): `/scalar/v1`
- Tests: `dotnet test`

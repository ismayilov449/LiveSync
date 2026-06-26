# Extending the platform

Checklist for adding a new domain aggregate (e.g. **Queues** and **Tickets** were added using this pattern when migrating from legacy Items/Categories).

## 1. Domain (`LiveSync.Domain`)

| Step | Location |
|------|----------|
| Entity + factory methods | `Entities/{Name}Aggregate/{Name}.cs` |
| Child entities (if any) | Inside aggregate folder (e.g. `TicketComment`) |
| Domain events | `Entities/{Name}Aggregate/Events/` |
| Repository interface | `Interfaces/Repositories/I{Name}Repository.cs` |
| Enums | `Enums/` (e.g. `TicketStatus`, `TicketPriority`) |

## 2. Application (`LiveSync.Application`)

| Step | Location |
|------|----------|
| Commands + handlers | `CQRS/{Names}/Commands/` |
| Queries + handlers | `CQRS/{Names}/Queries/` |
| FluentValidation | `CQRS/{Names}/Validators/` |
| DTOs | `CQRS/{Names}/Models/` |
| Cross-aggregate validators | e.g. `ITicketQueueValidator` |
| Enqueue domain events | `RealTimeSync/DomainEventHandlers/Enqueue{Name}*DomainEventHandler.cs` |
| Immediate API notify | `RealTimeSync/DomainEventHandlers/NotifyTenant{Name}DomainEventHandler.cs` |
| Worker change handler | `RealTimeSync/Handlers/{Name}ChangeHandler.cs` |
| Cache DTO | `RealTimeSync/ReadModels/{Name}CacheDto.cs` |
| Add `TopicBucket` value | `Domain/Enums/TopicBucket.cs` (Domain project) |

Register validators and handlers via assembly scanning in Infrastructure DI.

## 3. Infrastructure (`LiveSync.Infrastructure`)

| Step | Location |
|------|----------|
| EF configuration | `Persistence/Configurations/{Name}Configuration.cs` |
| Repository | `Persistence/Repositories/{Name}Repository.cs` |
| Bucket module | `RealTimeSync/Buckets/{Name}BucketModule.cs` |
| Register in `BucketModuleRegistry` | DI extension |
| Tenant bootstrap (if needed) | e.g. `TenantSupportDeskBootstrap` for default queue |
| Migration | `dotnet ef migrations add Add{Name}s --project LiveSync.Infrastructure --startup-project LiveSync.API` |

## 4. API (`LiveSync.API`)

| Step | Location |
|------|----------|
| Request contracts | `Contracts/{Names}/` |
| Mappings | `Mapping/{Name}RequestMappings.cs` |
| Controller | `Controllers/{Names}Controller.cs` |
| Audit calls | On create/update/delete/workflow in controller |

## 5. Client (`LiveSync.API/client`)

| Step | Location |
|------|----------|
| Types | `src/types/index.ts` |
| API wrapper | `src/api/index.ts` |
| Page | `src/pages/{Names}Page.tsx` |
| Push hook | Wrapper in `hooks/useSupportDeskPush.ts` using `useDomainPush` |
| List patch helpers | `utils/pushListPatch.ts` (sort + map from push change) |
| Route + nav | `App.tsx`, `Layout.tsx` |

## 6. Tests

| Layer | Location |
|-------|----------|
| Unit — handler | `LiveSync.Tests/{Name}CommandHandlerTests.cs` |
| Unit — validator | `LiveSync.Tests/{Name}ValidatorTests.cs` |
| Integration — CRUD | `LiveSync.IntegrationTests/{Names}IntegrationTests.cs` |
| Integration — isolation | `TenantIsolationIntegrationTests` |
| Integration — push | `BucketScopedPushIntegrationTests` pattern |
| Client | `client/src/utils/*.test.ts` (Vitest) |

## 7. Documentation

Update per [CONTRIBUTING.md](../CONTRIBUTING.md) doc map:

- `README.md` — API reference, routes, architecture bullets
- `docs/architecture.md` — tenant tables, SPA routes, domain model
- `docs/glossary.md` — new terms
- `docs/real-time-sync.md` — if push behavior changes
- `docs/demo-walkthrough.md` — new demo scenario if user-visible
- New ADR if the decision is non-obvious (see [ADR 006](adr/006-support-desk-aggregates.md))

## Reference implementation

Copy from **Tickets** and **Queues** side by side:

| Concern | Ticket | Queue |
|---------|--------|-------|
| Domain | `Ticket.cs`, `TicketComment.cs`, status machine | `Queue.cs`, deactivate rule |
| API | `TicketsController` — open, assign, comment, workflow | `QueuesController` — CRUD, deactivate |
| Real-time | `TicketBucketModule`, `TicketChangeHandler` | `QueueBucketModule`, `QueueChangeHandler` |
| Client | `TicketsPage.tsx` — list + detail + workflow | `QueuesPage.tsx` — list CRUD |
| Push hook | `useTicketsPush` | `useQueuesPush` |

## SignalR bucket contract

- Group name: `tenant:{tenantId}:bucket:{ticket|queue}` (`PushHubGroups.TenantBucket`)
- Client filter: `{bucket}.TenantId == {tenantId}` (lowercase bucket name in filter string)
- Entity id in push: `{bucket}-{numericId}` (e.g. `ticket-7`, `queue-3`)
- `TopicBucket` enum: `Ticket = 1`, `Queue = 2`

## Historical note

The platform originally used **Items** and **Categories** as scaffolding. [ADR 006](adr/006-support-desk-aggregates.md) replaced them with the Support Desk bounded context. ADRs 004–005 describe the multi-bucket pattern, which still applies with updated bucket names.

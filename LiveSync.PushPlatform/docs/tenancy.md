# Multi-tenancy

## Model

LiveSync uses **database-per-tenant** isolation:

- Each tenant gets `LiveSync_Tenant_{tenantId}` on the same SQL Server instance
- Users belong to exactly one tenant (`AspNetUsers.TenantId`)
- JWT includes `tenant_id` claim; middleware sets `ITenantContext` per request
- EF Core global query filters scope all item reads/writes to the active tenant

## Control plane vs tenant data

| Data | Location |
|------|----------|
| Users, roles, tenant registry, audit events | `LiveSync_ControlPlane` |
| Items, change queue, idempotency records | `LiveSync_Tenant_{id}` |

## Tenant lifecycle

| Status | Meaning |
|--------|---------|
| `Provisioning` | Database being created / migrated |
| `Active` | Normal operation |
| `Suspended` | Blocked for all API use except reactivate |

### Suspend and reactivate

| Action | Endpoint | Auth | Effect |
|--------|----------|------|--------|
| Suspend | `POST /api/v1/tenants/suspend` | TenantAdmin | Sets status `Suspended`; audit entry |
| Reactivate | `POST /api/v1/tenants/reactivate` | TenantAdmin | Sets status `Active`; audit entry |

When suspended:

- `TenantStatusMiddleware` returns **403** for authenticated requests
- **`POST /api/v1/tenants/reactivate` is still allowed** so an admin can recover
- Worker skips suspended tenants via `TenantRegistry` (only `Active` tenants polled)
- Admin **Settings** page in the SPA provides suspend/reactivate with confirmation

## Important implications

### Item IDs are per-tenant

Item `5` in tenant 1 is **not** the same as item `5` in tenant 2. Parent IDs must reference items that exist in **your** tenant.

### Register vs invite

| Action | Endpoint | Result |
|--------|----------|--------|
| Register | `POST /api/v1/auth/register` | Creates **new tenant** + `TenantAdmin` user |
| Invite | `POST /api/v1/auth/users` | Adds `TenantUser` to **caller's tenant** (admin only) |

Invite is available in the SPA under **Admin → Users** (previously on Profile).

### Root item bootstrap

Every tenant receives a **Root** item on provisioning so the hierarchy has a valid parent for new items.

## Tenant resolution pipeline

```
JWT validated
  → IUserContext.TenantId from claims
  → TenantAccessValidator (user belongs to tenant)
  → TenantStatusMiddleware (reject if Suspended, except reactivate)
  → ITenantContext.SetTenantId
  → AppDbContext resolves tenant connection string (Active tenants only)
  → Global query filter: Items.TenantId == active tenant
```

## Defense in depth

Even with separate databases, `Items.TenantId` is kept and validated on save for additional safety.

## Profile API

`GET /api/v1/auth/me` returns user fields plus `tenantName` and `tenantStatus` from the control plane — used by the Account and Admin sidebars.

## Connecting to databases

**Control plane** (local Docker):

```
Server=localhost,1433;Database=LiveSync_ControlPlane;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
```

**Tenant DB** example:

```
Server=localhost,1433;Database=LiveSync_Tenant_1;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
```

See `Tenancy:ConnectionTemplate` in `appsettings.json` for the pattern used at runtime.

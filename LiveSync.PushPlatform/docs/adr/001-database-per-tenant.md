# ADR 001: Database per tenant

## Status

Accepted

## Context

LiveSync is a multi-tenant SaaS platform storing hierarchical items per organization. We need strong isolation between tenants, predictable performance, and a path to scale individual tenants.

Options considered:

1. **Shared database, shared schema** — `TenantId` column on all tables
2. **Shared database, schema per tenant**
3. **Database per tenant**

## Decision

Use **database per tenant** with a central control plane database for identity, tenant registry, and audit events (`AuditEvents`).

Each tenant database is named `LiveSync_Tenant_{id}` and provisioned when a tenant registers.

## Consequences

### Positive

- Strong isolation boundary (backup, restore, export per tenant)
- No cross-tenant query leakage at the database level
- Easier compliance narrative for portfolio/demo purposes
- Tenant-specific migrations possible in future

### Negative

- More databases to manage and migrate on startup
- Connection pool multiplied by active tenants
- Cannot easily run cross-tenant analytics in SQL without federation

### Mitigations

- `TenantProvisioner` + `DatabaseInitializer` automate provisioning and migrations
- `TenantId` retained on tenant tables for defense in depth
- Control plane centralizes user/tenant metadata

## Alternatives rejected

**Shared schema** was rejected for this portfolio because it does not demonstrate the same isolation patterns required in regulated multi-tenant SaaS, though it remains valid for early-stage products with fewer tenants.

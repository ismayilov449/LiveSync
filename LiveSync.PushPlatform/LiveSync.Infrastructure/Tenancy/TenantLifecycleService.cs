using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantLifecycleService(
    MasterDbContext masterDb,
    ITenantConnectionResolver connectionResolver) : ITenantLifecycleService
{
    public async Task SuspendAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found.");

        if (tenant.Status == TenantStatus.Suspended)
            return;

        tenant.Status = TenantStatus.Suspended;
        await masterDb.SaveChangesAsync(ct);
        connectionResolver.InvalidateCache(tenantId);
    }

    public async Task ReactivateAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found.");

        if (tenant.Status == TenantStatus.Active)
            return;

        tenant.Status = TenantStatus.Active;
        await masterDb.SaveChangesAsync(ct);
        connectionResolver.InvalidateCache(tenantId);
    }
}

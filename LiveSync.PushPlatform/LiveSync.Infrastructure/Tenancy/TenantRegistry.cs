using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantRegistry(MasterDbContext masterDb) : ITenantRegistry
{
    public async Task<IReadOnlyList<int>> GetActiveTenantIdsAsync(CancellationToken ct = default)
        => await masterDb.Tenants
            .AsNoTracking()
            .Where(x => x.Status == TenantStatus.Active)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(int tenantId, CancellationToken ct = default)
        => masterDb.Tenants.AnyAsync(
            x => x.Id == tenantId && x.Status == TenantStatus.Active,
            ct);
}

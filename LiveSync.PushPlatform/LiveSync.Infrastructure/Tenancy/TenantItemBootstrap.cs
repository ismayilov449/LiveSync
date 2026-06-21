using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Entities.ItemAggregate;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantItemBootstrap(ITenantDbContextFactory tenantDbContextFactory) : ITenantItemBootstrap
{
    public async Task<int> EnsureRootItemAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = tenantDbContextFactory.CreateMigrationDbContext(tenantId);

        var existingRoot = await db.Items
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (existingRoot is not null)
            return existingRoot.Id;

        var root = Item.Create(tenantId, tenantId, "Root");
        db.Items.Add(root);
        await db.SaveChangesAsync(ct);
        return root.Id;
    }
}

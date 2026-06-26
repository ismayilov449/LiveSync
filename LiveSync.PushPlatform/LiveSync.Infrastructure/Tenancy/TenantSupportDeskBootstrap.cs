using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Entities.QueueAggregate;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantSupportDeskBootstrap(ITenantDbContextFactory tenantDbContextFactory)
    : ITenantSupportDeskBootstrap
{
    public async Task<int> EnsureDefaultQueueAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = tenantDbContextFactory.CreateMigrationDbContext(tenantId);

        var existing = await db.Queues
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing.Id;

        var queue = Queue.Create(tenantId, "General");
        db.Queues.Add(queue);
        await db.SaveChangesAsync(ct);
        return queue.Id;
    }
}

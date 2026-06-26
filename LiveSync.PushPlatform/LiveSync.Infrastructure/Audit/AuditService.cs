using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Audit;

public sealed class AuditService(MasterDbContext masterDb) : IAuditService
{
    public async Task RecordAsync(
        int tenantId,
        int userId,
        string action,
        string entityType,
        string? entityId,
        string? details,
        CancellationToken ct = default)
    {
        masterDb.AuditEvents.Add(new AuditEvent
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details?.Length > 2000 ? details[..2000] : details,
            CreatedAtUtc = DateTime.UtcNow
        });

        await masterDb.SaveChangesAsync(ct);
    }

    public async Task<PagedAuditEvents> ListAsync(int tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = masterDb.AuditEvents
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditEventDto(
                x.Id,
                x.TenantId,
                x.UserId,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.Details,
                x.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedAuditEvents(items, page, pageSize, total);
    }
}

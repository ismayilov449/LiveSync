using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.RealTimeSync;

public sealed class CacheDtoProvider(AppDbContext db, IFilterEvaluator filterEvaluator) : ICacheDtoProvider
{
    public async Task<ICacheDto?> FetchDtoAsync(int tenantId, TopicBucket bucket, int id, CancellationToken ct = default)
    {
        return bucket switch
        {
            TopicBucket.Item => await db.Items
                .Where(x => x.TenantId == tenantId && x.Id == id)
                .Select(x => new ItemCacheDto
                {
                    Id = x.Id,
                    TenantId = x.TenantId,
                    ParentId = x.ParentId,
                    Name = x.Name,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync(ct),
            _ => throw new NotImplementedException()
        };
    }

    public async Task<IReadOnlyList<ICacheDto>> FetchByFilterAsync(int tenantid, TopicBucket bucket, string filter, CancellationToken ct = default)
    {
        if (bucket != TopicBucket.Item)
            throw new NotSupportedException();

        var items = await db.Items
            .Where(x => x.TenantId == tenantid && x.IsActive)
            .Select(x => new ItemCacheDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                ParentId = x.ParentId,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return [.. items.Where(i => filterEvaluator.Matches(filter, i)).Cast<ICacheDto>()];
    }

}

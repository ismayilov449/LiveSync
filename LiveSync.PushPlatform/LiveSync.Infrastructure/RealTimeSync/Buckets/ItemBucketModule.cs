using System.Text.Json;
using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.RealTimeSync.Buckets;

public sealed class ItemBucketModule(AppDbContext db) : IBucketModule
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TopicBucket Bucket => TopicBucket.Item;
    public Type DtoClrType => typeof(ItemCacheDto);
    public string FilterParameterName => "item";

    public async Task<ICacheDto?> FetchDtoAsync(int tenantId, int id, CancellationToken ct = default)
        => await db.Items
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new ItemCacheDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                ParentId = x.ParentId,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ICacheDto>> FetchAllActiveAsync(int tenantId, CancellationToken ct = default)
    {
        var items = await db.Items
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .Select(x => new ItemCacheDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                ParentId = x.ParentId,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return items.Cast<ICacheDto>().ToList();
    }

    public ICacheDto? Deserialize(string json)
        => JsonSerializer.Deserialize<ItemCacheDto>(json, JsonOptions);
}

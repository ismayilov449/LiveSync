using System.Text.Json;
using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.RealTimeSync.Buckets;

public sealed class QueueBucketModule(AppDbContext db) : IBucketModule
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TopicBucket Bucket => TopicBucket.Queue;
    public Type DtoClrType => typeof(QueueCacheDto);
    public string FilterParameterName => "queue";

    public async Task<ICacheDto?> FetchDtoAsync(int tenantId, int id, CancellationToken ct = default)
        => await db.Queues
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new QueueCacheDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ICacheDto>> FetchAllActiveAsync(int tenantId, CancellationToken ct = default)
    {
        var queues = await db.Queues
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .Select(x => new QueueCacheDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Name = x.Name,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return queues.Cast<ICacheDto>().ToList();
    }

    public ICacheDto? Deserialize(string json)
        => JsonSerializer.Deserialize<QueueCacheDto>(json, JsonOptions);
}

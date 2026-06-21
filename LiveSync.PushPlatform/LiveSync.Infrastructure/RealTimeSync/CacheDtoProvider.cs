using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;

namespace LiveSync.Infrastructure.RealTimeSync;

public sealed class CacheDtoProvider(BucketModuleRegistry registry, IFilterEvaluator filterEvaluator)
    : ICacheDtoProvider
{
    public async Task<ICacheDto?> FetchDtoAsync(int tenantId, TopicBucket bucket, int id, CancellationToken ct = default)
        => await registry.GetRequired(bucket).FetchDtoAsync(tenantId, id, ct);

    public async Task<IReadOnlyList<ICacheDto>> FetchByFilterAsync(
        int tenantId,
        TopicBucket bucket,
        string filter,
        CancellationToken ct = default)
    {
        var module = registry.GetRequired(bucket);
        var dtos = await module.FetchAllActiveAsync(tenantId, ct);
        return dtos.Where(dto => filterEvaluator.Matches(filter, dto)).ToList();
    }
}

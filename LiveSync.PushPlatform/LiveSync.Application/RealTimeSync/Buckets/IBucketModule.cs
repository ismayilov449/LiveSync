using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.Buckets;

public interface IBucketModule
{
    TopicBucket Bucket { get; }
    Type DtoClrType { get; }
    string FilterParameterName { get; }

    Task<ICacheDto?> FetchDtoAsync(int tenantId, int id, CancellationToken ct = default);
    Task<IReadOnlyList<ICacheDto>> FetchAllActiveAsync(int tenantId, CancellationToken ct = default);
    ICacheDto? Deserialize(string json);
}

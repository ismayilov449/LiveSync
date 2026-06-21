using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface ICacheDtoProvider
{
    Task<ICacheDto?> FetchDtoAsync(int tenantId, TopicBucket bucket, int id, CancellationToken ct = default);
    Task<IReadOnlyList<ICacheDto>> FetchByFilterAsync(int tenantid, TopicBucket bucket, string filter, CancellationToken ct = default);
}

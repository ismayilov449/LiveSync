using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface ITopicDataCache
{
    Task<IDictionary<string, ICacheDto>> GetAllAsync(Topic topic, CancellationToken ct = default);
    Task SetAllAsync(Topic topic, IDictionary<string, ICacheDto> data, CancellationToken ct = default);
    Task UpsertAsync(Topic topic, string froentEndId, ICacheDto data, CancellationToken ct = default);
    Task<bool> DeleteAsync(Topic topic, string froentEndId, CancellationToken ct = default);
    Task DeleteTopicAsync(Topic topic, CancellationToken ct = default);
}

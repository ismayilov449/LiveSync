using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface ISubscriptionStore
{
    Task AddAsync(SubscriptionDto subscription, CancellationToken ct = default);
    Task RemoveAsync(string subscriptionId, int tenantId, CancellationToken ct = default);
    Task RemoveByConnectionAsync(string connectionId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionDto>> GetByConnectionIdAsync(string connectionId, int tenantId, CancellationToken ct = default);
    Task<SubscriptionDto?> GetByIdAsync(string subscriptionId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionDto>> GetByTopicHashAsync(int tenantId, string topicHash, CancellationToken ct = default);
    Task<IReadOnlyList<Topic>> GetTopicsByBucketAsync(int tenantId, TopicBucket bucket, CancellationToken ct = default);
    Task RenewAsync(string subscriptionId, int tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetExpiredSubscriptionKeysAsync(DateTimeOffset olderThan, CancellationToken ct = default);
    Task RemoveTopicFromBucketAsync(int tenantId, Topic topic, CancellationToken ct = default);
}

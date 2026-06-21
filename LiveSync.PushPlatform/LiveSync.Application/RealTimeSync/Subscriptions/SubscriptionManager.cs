using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.Subscriptions;

public sealed class SubscriptionManager(
    ISubscriptionStore subscriptionStore,
    ITopicDataCache topicDataCache,
    ICacheDtoProvider cacheDtoProvider,
    IFilterEvaluator filterEvaluator
    )
{
    public async Task<FindAndSubscribeResponse> FindAndSubscribeAsync(
        int tenantId,
        int userId,
        string connectionId,
        string bucket,
        string filter,
        CancellationToken ct = default)
    {
        if (!filterEvaluator.IsValidFilter(filter))
            throw new ArgumentException("Invalid filter expression.", nameof(filter));

        var topicBucket = Enum.Parse<TopicBucket>(bucket, ignoreCase: true);
        var topic = new Topic(tenantId, topicBucket, filter);

        var existingTopics = await subscriptionStore.GetTopicsByBucketAsync(tenantId, topicBucket, ct);
        var topicAlreadyWarm = existingTopics.Any(t => t.Hash == topic.Hash);

        IDictionary<string, ICacheDto> data;

        if (topicAlreadyWarm)
        {
            data = await topicDataCache.GetAllAsync(topic, ct);
        }
        else
        {
            var dtos = await cacheDtoProvider.FetchByFilterAsync(tenantId, topicBucket, filter, ct);
            data = dtos.ToDictionary(dto => dto.FrontEndId, dto => dto);
            await topicDataCache.SetAllAsync(topic, data, ct);
        }

        var subscription = new SubscriptionDto
        {
            Id = Guid.NewGuid().ToString("N"),
            ConnectionId = connectionId,
            TenantId = tenantId,
            UserId = userId,
            Topic = topic
        };

        await subscriptionStore.AddAsync(subscription, ct);

        return new FindAndSubscribeResponse
        {
            SubscriptionId = subscription.Id,
            Data = data.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
        };
    }

    public async Task UnsubscribeAsync(string subscriptionId, int tenantId, CancellationToken ct = default)
    {
        var sub = await subscriptionStore.GetByIdAsync(subscriptionId, tenantId, ct);
        if (sub is null)
            return;

        await subscriptionStore.RemoveAsync(subscriptionId, tenantId, ct);
        await CleanupTopicIfEmptyAsync(tenantId, sub.Topic, ct);
    }

    public Task RenewAsync(string subscriptionId, int tenantId, CancellationToken ct = default)
        => subscriptionStore.RenewAsync(subscriptionId, tenantId, ct);

    public async Task RemoveConnectionAsync(string connectionId, int tenantId, CancellationToken ct = default)
    {
        var subs = await subscriptionStore.GetByConnectionIdAsync(connectionId, tenantId, ct);
        await subscriptionStore.RemoveByConnectionAsync(connectionId, tenantId, ct);

        foreach (var sub in subs)
            await CleanupTopicIfEmptyAsync(tenantId, sub.Topic, ct);
    }

    private async Task CleanupTopicIfEmptyAsync(int tenantId, Topic topic, CancellationToken ct)
    {
        var remaining = await subscriptionStore.GetByTopicHashAsync(tenantId, topic.Hash, ct);
        if (remaining.Count > 0)
            return;

        await topicDataCache.DeleteTopicAsync(topic, ct);
        await subscriptionStore.RemoveTopicFromBucketAsync(tenantId, topic, ct);
    }
}

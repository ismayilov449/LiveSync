using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Enums;
using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.Handlers;

public sealed class ItemChangeHandler(
    ICacheDtoProvider cacheDtoProvider,
    ISubscriptionStore subscriptionStore,
    ITopicDataCache topicDataCache,
    IFilterEvaluator filterEvaluator,
    IRealTimeNotifier notifier) : IChangeHandler
{
    public TopicBucket Bucket => TopicBucket.Item;

    public async Task HandleAsync(ChangeEnvelope change, CancellationToken ct = default)
    {
        if (change.EventType == ChangeType.Delete)
        {
            await ProcessDeleteAsync(change, ct);
            return;
        }

        var dto = await cacheDtoProvider.FetchDtoAsync(change.TenantId, change.Bucket, change.EntityId, ct);
        if (dto is null)
            return;

        if (dto is ItemCacheDto { IsActive: false })
        {
            await ProcessDeleteAsync(change with { EventType = ChangeType.Delete }, ct);
            return;
        }

        var topics = await subscriptionStore.GetTopicsByBucketAsync(change.TenantId, change.Bucket, ct);
        foreach (var topic in topics)
        {
            if (filterEvaluator.Matches(topic.Filter, dto))
                await topicDataCache.UpsertAsync(topic, dto.FrontEndId, dto, ct);
            else
                await topicDataCache.DeleteAsync(topic, dto.FrontEndId, ct);
        }

        await NotifyTenantAsync(change.TenantId, dto.FrontEndId, NotificationOperation.Upsert, dto, ct);
    }

    private async Task ProcessDeleteAsync(ChangeEnvelope change, CancellationToken ct)
    {
        var topics = await subscriptionStore.GetTopicsByBucketAsync(change.TenantId, change.Bucket, ct);
        var frontendId = $"{change.Bucket.ToString().ToLowerInvariant()}-{change.EntityId}";

        foreach (var topic in topics)
            await topicDataCache.DeleteAsync(topic, frontendId, ct);

        await NotifyTenantAsync(change.TenantId, frontendId, NotificationOperation.Delete, null, ct);
    }

    private Task NotifyTenantAsync(
        int tenantId,
        string frontendId,
        NotificationOperation operation,
        object? dto,
        CancellationToken ct)
    {
        var payload = new ChangeNotificationDto
        {
            Operation = operation,
            Entity = new ChangeEntityDto
            {
                Bucket = TopicBucket.Item.ToString().ToLowerInvariant(),
                Id = frontendId
            },
            Change = dto
        };

        return notifier.NotifyTenantAsync(tenantId, payload, ct);
    }
}

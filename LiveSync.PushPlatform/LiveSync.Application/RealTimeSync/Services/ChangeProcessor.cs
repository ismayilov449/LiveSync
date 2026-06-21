using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Enums;
using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.Services;

public sealed class ChangeProcessor(
    ICacheDtoProvider cacheDtoProvider,
    ISubscriptionStore subscriptionStore,
    ITopicDataCache topicDataCache,
    IFilterEvaluator filterEvaluator,
    IRealTimeNotifier notifier
    )
{
    public async Task ProcessAsync(ChangeEnvelope change, CancellationToken ct = default)
    {
        if (change.EventType == ChangeType.Delete)
        {
            await ProcessDeleteAsync(change, ct);
            return;
        }

        var dto = await cacheDtoProvider.FetchDtoAsync(change.TenantId, change.Bucket, change.EntityId, ct);
        if (dto is null)
        {
            return;
        }

        if (dto is ItemCacheDto { IsActive: false })
        {
            await ProcessDeleteAsync(change with { EventType = ChangeType.Delete }, ct);
            return;
        }

        var topics = await subscriptionStore.GetTopicsByBucketAsync(change.TenantId, change.Bucket, ct);
        foreach (var topic in topics)
        {
            var mathches = filterEvaluator.Matches(topic.Filter, dto);

            if (mathches)
            {
                await topicDataCache.UpsertAsync(topic, dto.FrontEndId, dto, ct);
                await NotifyTopicAsync(topic, dto.FrontEndId, NotificationOperation.Upsert, dto, ct);
            }
            else
            {
                var deleted = await topicDataCache.DeleteAsync(topic, dto.FrontEndId, ct);
                if (deleted)
                {
                    await NotifyTopicAsync(topic, dto.FrontEndId, NotificationOperation.Delete, null, ct);
                }
            }
        }
    }

    private async Task ProcessDeleteAsync(ChangeEnvelope change, CancellationToken ct)
    {
        var topics = await subscriptionStore.GetTopicsByBucketAsync(change.TenantId, change.Bucket, ct);
        var frontendId = $"{change.Bucket.ToString().ToLowerInvariant()}-{change.EntityId}";

        foreach (var topic in topics)
        {
            var deleted = await topicDataCache.DeleteAsync(topic, frontendId, ct);
            if (deleted)
            {
                await NotifyTopicAsync(topic, frontendId, NotificationOperation.Delete, null, ct);
            }
        }
    }

    private async Task NotifyTopicAsync(Topic topic, string frontendId, NotificationOperation operation, object? dto, CancellationToken ct)
    {
        var subs = await subscriptionStore.GetByTopicHashAsync(topic.TenantId, topic.Hash, ct);
        var paylaod = new ChangeNotificationDto
        {
            Operation = operation,
            Entity = new ChangeEntityDto
            {
                Bucket = topic.Bucket.ToString().ToLowerInvariant(),
                Id = frontendId
            },
            Change = dto
        };

        foreach (var sub in subs)
        {
            await notifier.NotifyAsync(sub.ConnectionId, paylaod, ct);
        }
    }

}

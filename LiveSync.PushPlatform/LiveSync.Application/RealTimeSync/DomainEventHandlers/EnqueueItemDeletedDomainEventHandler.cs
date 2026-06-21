using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.ItemAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueItemDeletedDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<ItemDeletedDomainEvent>
{
    public Task Handle(ItemDeletedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Item:tenantId#{notification.TenantId}:eventType#Delete",
            Payload = $"{{\"id\":{notification.ItemId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Item,
            TenantId = notification.TenantId,
            EventType = ChangeType.Delete,
            EntityId = notification.ItemId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

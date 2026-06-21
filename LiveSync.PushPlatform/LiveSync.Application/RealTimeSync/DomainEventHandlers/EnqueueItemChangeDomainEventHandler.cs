using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.ItemAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueItemChangeDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<ItemUpdatedDomainEvent>
{
    public Task Handle(ItemUpdatedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Item:tenantId#{notification.TenantId}:eventType#Update",
            Payload = $"{{\"id\":{notification.ItemId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Item,
            TenantId = notification.TenantId,
            EventType = ChangeType.Update,
            EntityId = notification.ItemId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

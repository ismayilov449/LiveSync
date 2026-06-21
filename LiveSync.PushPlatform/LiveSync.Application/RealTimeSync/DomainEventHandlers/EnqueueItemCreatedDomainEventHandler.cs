using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.ItemAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueItemCreatedDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<ItemCreatedDomainEvent>
{
    public Task Handle(ItemCreatedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Item:tenantId#{notification.TenantId}:eventType#Insert",
            Payload = $"{{\"id\":{notification.ItemId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Item,
            TenantId = notification.TenantId,
            EventType = ChangeType.Insert,
            EntityId = notification.ItemId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

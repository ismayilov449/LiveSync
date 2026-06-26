using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.QueueAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueQueueChangeDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<QueueUpdatedDomainEvent>
{
    public Task Handle(QueueUpdatedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Queue:tenantId#{notification.TenantId}:eventType#Update",
            Payload = $"{{\"id\":{notification.QueueId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Queue,
            TenantId = notification.TenantId,
            EventType = ChangeType.Update,
            EntityId = notification.QueueId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

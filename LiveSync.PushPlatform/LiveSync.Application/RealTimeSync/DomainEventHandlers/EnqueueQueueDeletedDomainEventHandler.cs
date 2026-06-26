using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.QueueAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueQueueDeletedDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<QueueDeletedDomainEvent>
{
    public Task Handle(QueueDeletedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Queue:tenantId#{notification.TenantId}:eventType#Delete",
            Payload = $"{{\"id\":{notification.QueueId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Queue,
            TenantId = notification.TenantId,
            EventType = ChangeType.Delete,
            EntityId = notification.QueueId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

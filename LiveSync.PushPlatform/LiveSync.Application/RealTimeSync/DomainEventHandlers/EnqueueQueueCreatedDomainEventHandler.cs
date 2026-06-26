using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.QueueAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueQueueCreatedDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<QueueCreatedDomainEvent>
{
    public Task Handle(QueueCreatedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Queue:tenantId#{notification.TenantId}:eventType#Insert",
            Payload = $"{{\"id\":{notification.QueueId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Queue,
            TenantId = notification.TenantId,
            EventType = ChangeType.Insert,
            EntityId = notification.QueueId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

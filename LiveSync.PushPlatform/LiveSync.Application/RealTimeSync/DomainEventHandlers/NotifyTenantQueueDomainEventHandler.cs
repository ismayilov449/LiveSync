using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Enums;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.QueueAggregate.Events;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class NotifyTenantQueueDomainEventHandler(IRealTimeNotifier notifier)
    : INotificationHandler<QueueCreatedDomainEvent>,
        INotificationHandler<QueueUpdatedDomainEvent>,
        INotificationHandler<QueueDeletedDomainEvent>
{
    public Task Handle(QueueCreatedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.QueueId, NotificationOperation.Upsert, ct);

    public Task Handle(QueueUpdatedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.QueueId, NotificationOperation.Upsert, ct);

    public Task Handle(QueueDeletedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.QueueId, NotificationOperation.Delete, ct);

    private Task NotifyAsync(
        int tenantId,
        int queueId,
        NotificationOperation operation,
        CancellationToken ct)
    {
        var frontendId = new FrontEndId(TopicBucket.Queue, queueId).Value;
        var payload = new ChangeNotificationDto
        {
            Operation = operation,
            Entity = new ChangeEntityDto
            {
                Bucket = TopicBucket.Queue.ToString().ToLowerInvariant(),
                Id = frontendId
            }
        };

        return notifier.NotifyBucketAsync(tenantId, TopicBucket.Queue, payload, ct);
    }
}

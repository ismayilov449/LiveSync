using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Enums;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.ItemAggregate.Events;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class NotifyTenantItemDomainEventHandler(IRealTimeNotifier notifier)
    : INotificationHandler<ItemCreatedDomainEvent>,
        INotificationHandler<ItemUpdatedDomainEvent>,
        INotificationHandler<ItemDeletedDomainEvent>
{
    public Task Handle(ItemCreatedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.ItemId, NotificationOperation.Upsert, ct);

    public Task Handle(ItemUpdatedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.ItemId, NotificationOperation.Upsert, ct);

    public Task Handle(ItemDeletedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.ItemId, NotificationOperation.Delete, ct);

    private Task NotifyAsync(
        int tenantId,
        int itemId,
        NotificationOperation operation,
        CancellationToken ct)
    {
        var frontendId = new FrontEndId(TopicBucket.Item, itemId).Value;
        var payload = new ChangeNotificationDto
        {
            Operation = operation,
            Entity = new ChangeEntityDto
            {
                Bucket = TopicBucket.Item.ToString().ToLowerInvariant(),
                Id = frontendId
            }
        };

        return notifier.NotifyTenantAsync(tenantId, payload, ct);
    }
}

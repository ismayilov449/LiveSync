using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Enums;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.TicketAggregate.Events;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class NotifyTenantTicketDomainEventHandler(IRealTimeNotifier notifier)
    : INotificationHandler<TicketOpenedDomainEvent>,
        INotificationHandler<TicketAssignedDomainEvent>,
        INotificationHandler<TicketCommentAddedDomainEvent>,
        INotificationHandler<TicketStatusChangedDomainEvent>,
        INotificationHandler<TicketDeletedDomainEvent>
{
    public Task Handle(TicketOpenedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.TicketId, NotificationOperation.Upsert, ct);

    public Task Handle(TicketAssignedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.TicketId, NotificationOperation.Upsert, ct);

    public Task Handle(TicketCommentAddedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.TicketId, NotificationOperation.Upsert, ct);

    public Task Handle(TicketStatusChangedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.TicketId, NotificationOperation.Upsert, ct);

    public Task Handle(TicketDeletedDomainEvent notification, CancellationToken ct)
        => NotifyAsync(notification.TenantId, notification.TicketId, NotificationOperation.Delete, ct);

    private Task NotifyAsync(
        int tenantId,
        int ticketId,
        NotificationOperation operation,
        CancellationToken ct)
    {
        var frontendId = new FrontEndId(TopicBucket.Ticket, ticketId).Value;
        var payload = new ChangeNotificationDto
        {
            Operation = operation,
            Entity = new ChangeEntityDto
            {
                Bucket = TopicBucket.Ticket.ToString().ToLowerInvariant(),
                Id = frontendId
            }
        };

        return notifier.NotifyBucketAsync(tenantId, TopicBucket.Ticket, payload, ct);
    }
}

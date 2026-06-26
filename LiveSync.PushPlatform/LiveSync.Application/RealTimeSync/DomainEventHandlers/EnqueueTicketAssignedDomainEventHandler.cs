using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.TicketAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueTicketAssignedDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<TicketAssignedDomainEvent>
{
    public Task Handle(TicketAssignedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Ticket:tenantId#{notification.TenantId}:eventType#Update",
            Payload = $"{{\"id\":{notification.TicketId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Ticket,
            TenantId = notification.TenantId,
            EventType = ChangeType.Update,
            EntityId = notification.TicketId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

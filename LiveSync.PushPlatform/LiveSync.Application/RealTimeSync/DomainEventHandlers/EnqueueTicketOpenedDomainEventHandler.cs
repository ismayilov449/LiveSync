using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Entities.TicketAggregate.Events;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Application.RealTimeSync.DomainEventHandlers;

public sealed class EnqueueTicketOpenedDomainEventHandler(IChangeQueueStore queue)
    : INotificationHandler<TicketOpenedDomainEvent>
{
    public Task Handle(TicketOpenedDomainEvent notification, CancellationToken ct)
    {
        var envelope = new ChangeEnvelope
        {
            Key = $"table#Ticket:tenantId#{notification.TenantId}:eventType#Insert",
            Payload = $"{{\"id\":{notification.TicketId}}}",
            CreatedAt = DateTimeOffset.UtcNow,
            Bucket = TopicBucket.Ticket,
            TenantId = notification.TenantId,
            EventType = ChangeType.Insert,
            EntityId = notification.TicketId
        };
        return queue.EnqueueAsync(envelope, ct);
    }
}

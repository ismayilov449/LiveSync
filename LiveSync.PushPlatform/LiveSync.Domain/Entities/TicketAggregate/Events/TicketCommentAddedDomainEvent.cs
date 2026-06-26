using LiveSync.Domain.Common;
using MediatR;

namespace LiveSync.Domain.Entities.TicketAggregate.Events;

public sealed class TicketCommentAddedDomainEvent(int tenantId, int ticketId) : DomainEvent, INotification
{
    public int TenantId { get; } = tenantId;
    public int TicketId { get; } = ticketId;
}

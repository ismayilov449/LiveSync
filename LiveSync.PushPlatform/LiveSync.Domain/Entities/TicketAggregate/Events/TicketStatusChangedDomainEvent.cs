using LiveSync.Domain.Common;
using LiveSync.Domain.Enums;
using MediatR;

namespace LiveSync.Domain.Entities.TicketAggregate.Events;

public sealed class TicketStatusChangedDomainEvent(int tenantId, int ticketId, TicketStatus status) : DomainEvent, INotification
{
    public int TenantId { get; } = tenantId;
    public int TicketId { get; } = ticketId;
    public TicketStatus Status { get; } = status;
}

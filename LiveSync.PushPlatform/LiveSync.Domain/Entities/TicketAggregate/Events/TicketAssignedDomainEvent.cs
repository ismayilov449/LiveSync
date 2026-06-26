using LiveSync.Domain.Common;
using MediatR;

namespace LiveSync.Domain.Entities.TicketAggregate.Events;

public sealed class TicketAssignedDomainEvent(int tenantId, int ticketId, int assigneeUserId) : DomainEvent, INotification
{
    public int TenantId { get; } = tenantId;
    public int TicketId { get; } = ticketId;
    public int AssigneeUserId { get; } = assigneeUserId;
}

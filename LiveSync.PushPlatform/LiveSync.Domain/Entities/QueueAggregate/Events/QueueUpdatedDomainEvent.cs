using LiveSync.Domain.Common;
using MediatR;

namespace LiveSync.Domain.Entities.QueueAggregate.Events;

public sealed class QueueUpdatedDomainEvent(int tenantId, int queueId) : DomainEvent, INotification
{
    public int TenantId { get; } = tenantId;
    public int QueueId { get; } = queueId;
}

using LiveSync.Domain.Common;
using MediatR;

namespace LiveSync.Domain.Entities.ItemAggregate.Events;

public sealed class ItemDeletedDomainEvent(int tenantId, int itemId) : DomainEvent, INotification
{
    public int TenantId { get; } = tenantId;
    public int ItemId { get; } = itemId;
}

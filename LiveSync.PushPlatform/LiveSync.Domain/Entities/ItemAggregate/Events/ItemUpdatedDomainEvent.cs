using LiveSync.Domain.Common;
using MediatR;

namespace LiveSync.Domain.Entities.ItemAggregate.Events;

public sealed class ItemUpdatedDomainEvent(int tenantId, int itemId) : DomainEvent, INotification
{
    public int TenantId { get; set; } = tenantId;
    public int ItemId { get; set; } = itemId;
}

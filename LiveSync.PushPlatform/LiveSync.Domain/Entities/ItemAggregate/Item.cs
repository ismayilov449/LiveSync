using LiveSync.Domain.Common;
using LiveSync.Domain.Entities.ItemAggregate.Events;
using LiveSync.Domain.Interfaces;

namespace LiveSync.Domain.Entities.ItemAggregate;

public sealed class Item : AggregateRoot, IAggregateRoot
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public int ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Item() { }

    private Item(int tenantId, int parentId, string name)
    {
        TenantId = tenantId;
        ParentId = parentId;
        Name = name;
        IsActive = true;
        var now = DateTime.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Item Create(int tenantId, int parentId, string name)
    {
        return new Item(tenantId, parentId, name);
    }

    public void CompleteCreation()
    {
        if (Id <= 0)
            throw new InvalidOperationException("Item id must be assigned before completing creation.");

        Raise(new ItemCreatedDomainEvent(TenantId, Id));
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        Raise(new ItemUpdatedDomainEvent(TenantId, Id));
    }

    public void MoveToParent(int parentId)
    {
        if (parentId <= 0)
            throw new ArgumentException("ParentId is required.");

        ParentId = parentId;
        UpdatedAtUtc = DateTime.UtcNow;
        Raise(new ItemUpdatedDomainEvent(TenantId, Id));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
        Raise(new ItemUpdatedDomainEvent(TenantId, Id));
    }

    public void MarkDeleted()
    {
        Raise(new ItemDeletedDomainEvent(TenantId, Id));
    }

}

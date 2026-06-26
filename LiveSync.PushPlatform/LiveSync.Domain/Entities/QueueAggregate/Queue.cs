using LiveSync.Domain.Common;
using LiveSync.Domain.Entities.QueueAggregate.Events;
using LiveSync.Domain.Interfaces;

namespace LiveSync.Domain.Entities.QueueAggregate;

public sealed class Queue : AggregateRoot, IAggregateRoot
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Queue() { }

    private Queue(int tenantId, string name)
    {
        TenantId = tenantId;
        Name = name;
        IsActive = true;
        var now = DateTime.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Queue Create(int tenantId, string name)
        => new(tenantId, name);

    public void CompleteCreation()
    {
        if (Id <= 0)
            throw new InvalidOperationException("Queue id must be assigned before completing creation.");

        Raise(new QueueCreatedDomainEvent(TenantId, Id));
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        Raise(new QueueUpdatedDomainEvent(TenantId, Id));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
        Raise(new QueueUpdatedDomainEvent(TenantId, Id));
    }

    public void MarkDeleted()
        => Raise(new QueueDeletedDomainEvent(TenantId, Id));
}

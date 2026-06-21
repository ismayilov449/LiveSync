namespace LiveSync.Domain.Common;

public abstract class DomainEvent
{
    public DateTime OccuredOnUtc { get; } = DateTime.UtcNow;
}

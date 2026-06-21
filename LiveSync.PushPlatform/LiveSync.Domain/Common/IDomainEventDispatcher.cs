namespace LiveSync.Domain.Common;

public interface IDomainEventDispatcher
{
    Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken ct = default);
}

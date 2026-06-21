using LiveSync.Domain.Common;
using LiveSync.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Persistence;

public sealed class UnitOfWork(
    AppDbContext db,
    IDomainEventDispatcher domainEventDispatcher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entitiesWithEvents = GetEntitiesWithEvents();
        var result = await db.SaveChangesAsync(ct);
        await domainEventDispatcher.DispatchAndClearEventsAsync(entitiesWithEvents, ct);
        return result;
    }

    public Task PublishDomainEventsAsync(IEnumerable<Entity> entities, CancellationToken ct = default)
        => domainEventDispatcher.DispatchAndClearEventsAsync(entities, ct);

    private List<Entity> GetEntitiesWithEvents()
        => db.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();
}
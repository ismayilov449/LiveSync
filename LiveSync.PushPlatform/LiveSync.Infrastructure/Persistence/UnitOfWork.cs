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
        var entitiesWithEvents = db.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var result = await db.SaveChangesAsync(ct);

        await domainEventDispatcher.DispatchAndClearEventsAsync(entitiesWithEvents, ct);

        return result;
    }
}
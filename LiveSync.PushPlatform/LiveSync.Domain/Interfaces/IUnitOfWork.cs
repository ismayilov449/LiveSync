using LiveSync.Domain.Common;

namespace LiveSync.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task PublishDomainEventsAsync(IEnumerable<Entity> entities, CancellationToken ct = default);
}

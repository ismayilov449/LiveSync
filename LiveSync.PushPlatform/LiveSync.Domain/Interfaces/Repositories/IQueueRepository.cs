using LiveSync.Domain.Entities.QueueAggregate;
using System.Linq.Expressions;

namespace LiveSync.Domain.Interfaces.Repositories;

public interface IQueueRepository : IRepository<Queue>, IReadRepository<Queue>
{
    Task<Queue?> GetByTenantAndIdAsync(int tenantId, int id, CancellationToken ct = default);

    Task<PagedResult<Queue>> ListPagedAsync(
        Expression<Func<Queue, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

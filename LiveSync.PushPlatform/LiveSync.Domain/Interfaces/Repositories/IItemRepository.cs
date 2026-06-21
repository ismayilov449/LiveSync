using LiveSync.Domain.Entities.ItemAggregate;
using System.Linq.Expressions;

namespace LiveSync.Domain.Interfaces.Repositories;

public interface IItemRepository : IRepository<Item>, IReadRepository<Item>
{
    Task<Item?> GetByTenantAndIdAsync(int tenantId, int id, CancellationToken ct = default);

    Task<PagedResult<Item>> ListPagedAsync(
        Expression<Func<Item, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

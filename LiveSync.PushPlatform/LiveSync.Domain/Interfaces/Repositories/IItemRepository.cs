using LiveSync.Domain.Entities.ItemAggregate;

namespace LiveSync.Domain.Interfaces.Repositories;

public interface IItemRepository : IRepository<Item>, IReadRepository<Item>
{
    Task<Item?> GetByTenantAndIdAsync(int tenantId, int id, CancellationToken ct = default);
}

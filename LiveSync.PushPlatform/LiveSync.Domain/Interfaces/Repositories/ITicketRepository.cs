using LiveSync.Domain.Entities.TicketAggregate;
using LiveSync.Domain.Enums;
using System.Linq.Expressions;

namespace LiveSync.Domain.Interfaces.Repositories;

public interface ITicketRepository : IRepository<Ticket>, IReadRepository<Ticket>
{
    Task<Ticket?> GetByTenantAndIdAsync(int tenantId, int id, CancellationToken ct = default);

    Task<Ticket?> GetByTenantAndIdWithCommentsAsync(int tenantId, int id, CancellationToken ct = default);

    Task<int> CountOpenInQueueAsync(int tenantId, int queueId, CancellationToken ct = default);

    Task<PagedResult<Ticket>> ListPagedAsync(
        Expression<Func<Ticket, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

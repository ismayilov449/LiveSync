using LiveSync.Domain.Entities.QueueAggregate;
using LiveSync.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LiveSync.Infrastructure.Persistence.Repositories;

public sealed class QueueRepository(AppDbContext db) : IQueueRepository
{
    public Task<Queue?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.Queues.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Queue?> GetByTenantAndIdAsync(int tenantId, int id, CancellationToken ct = default)
        => db.Queues.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

    public Task AddAsync(Queue entity, CancellationToken ct = default)
    {
        db.Queues.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Queue entity, CancellationToken ct = default)
    {
        db.Queues.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Queue entity, CancellationToken ct = default)
    {
        db.Queues.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<Queue?> FirstOrDefaultAsync(Expression<Func<Queue, bool>> predicate, CancellationToken ct = default)
        => db.Queues.FirstOrDefaultAsync(predicate, ct);

    public Task<List<Queue>> ListAsync(Expression<Func<Queue, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null
            ? db.Queues.ToListAsync(ct)
            : db.Queues.Where(predicate).ToListAsync(ct);

    public async Task<PagedResult<Queue>> ListPagedAsync(
        Expression<Func<Queue, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Queues.Where(predicate);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Queue>(items, totalCount);
    }
}

using LiveSync.Domain.Entities.TicketAggregate;
using LiveSync.Domain.Enums;
using LiveSync.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LiveSync.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository(AppDbContext db) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.Tickets.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Ticket?> GetByTenantAndIdAsync(int tenantId, int id, CancellationToken ct = default)
        => db.Tickets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

    public Task<Ticket?> GetByTenantAndIdWithCommentsAsync(int tenantId, int id, CancellationToken ct = default)
        => db.Tickets
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

    public Task<int> CountOpenInQueueAsync(int tenantId, int queueId, CancellationToken ct = default)
        => db.Tickets.CountAsync(
            x => x.TenantId == tenantId && x.QueueId == queueId && x.Status != TicketStatus.Closed,
            ct);

    public Task AddAsync(Ticket entity, CancellationToken ct = default)
    {
        db.Tickets.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Ticket entity, CancellationToken ct = default)
    {
        db.Tickets.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Ticket entity, CancellationToken ct = default)
    {
        db.Tickets.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<Ticket?> FirstOrDefaultAsync(Expression<Func<Ticket, bool>> predicate, CancellationToken ct = default)
        => db.Tickets.FirstOrDefaultAsync(predicate, ct);

    public Task<List<Ticket>> ListAsync(Expression<Func<Ticket, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null
            ? db.Tickets.ToListAsync(ct)
            : db.Tickets.Where(predicate).ToListAsync(ct);

    public async Task<PagedResult<Ticket>> ListPagedAsync(
        Expression<Func<Ticket, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Tickets.Where(predicate);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Ticket>(items, totalCount);
    }
}

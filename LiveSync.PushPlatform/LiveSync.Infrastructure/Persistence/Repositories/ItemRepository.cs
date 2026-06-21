using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LiveSync.Infrastructure.Persistence.Repositories;

public sealed class ItemRepository(AppDbContext db) : IItemRepository
{
    public Task<Item?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.Items.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Item?> GetByTenantAndIdAsync(int tenantId, int id, CancellationToken ct = default)
        => db.Items.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

    public Task AddAsync(Item entity, CancellationToken ct = default)
    {
        db.Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Item entity, CancellationToken ct = default)
    {
        db.Items.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Item entity, CancellationToken ct = default)
    {
        db.Items.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<Item?> FirstOrDefaultAsync(Expression<Func<Item, bool>> predicate, CancellationToken ct = default)
        => db.Items.FirstOrDefaultAsync(predicate, ct);

    public Task<List<Item>> ListAsync(Expression<Func<Item, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null
            ? db.Items.ToListAsync(ct)
            : db.Items.Where(predicate).ToListAsync(ct);

    public async Task<PagedResult<Item>> ListPagedAsync(
        Expression<Func<Item, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Items.Where(predicate);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Item>(items, totalCount);
    }
}
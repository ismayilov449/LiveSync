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
}
using System.Linq.Expressions;

namespace LiveSync.Domain.Interfaces;

public interface IReadRepository<T> where T : class, IAggregateRoot
{
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate, CancellationToken ct = default);
}

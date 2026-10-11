using LiveSync.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Persistence;

public sealed class EfTransactionRunner(AppDbContext db) : ITransactionRunner
{
    public Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null)
            return operation(ct);

        var attempt = 0;
        var strategy = db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async token =>
        {
            // A retry re-runs the whole command, so forget entities tracked by the failed attempt.
            if (attempt++ > 0)
                db.ChangeTracker.Clear();

            await using var transaction = await db.Database.BeginTransactionAsync(token);
            var result = await operation(token);
            await transaction.CommitAsync(token);
            return result;
        }, ct);
    }
}

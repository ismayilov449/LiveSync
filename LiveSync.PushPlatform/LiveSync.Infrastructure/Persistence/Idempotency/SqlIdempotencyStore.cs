using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Persistence.Idempotency;

public sealed class SqlIdempotencyStore(AppDbContext db) : IIdempotencyStore
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    public async Task<IdempotencyResult?> TryGetAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var now = DateTime.UtcNow;
        var record = await db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key && x.ExpiresAtUtc > now, ct);

        return record is null
            ? null
            : new IdempotencyResult(record.ResourceId, WasReplayed: true);
    }

    public async Task SaveAsync(string key, int resourceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var now = DateTime.UtcNow;
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            ResourceId = resourceId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(DefaultTtl)
        });

        await db.SaveChangesAsync(ct);
    }
}

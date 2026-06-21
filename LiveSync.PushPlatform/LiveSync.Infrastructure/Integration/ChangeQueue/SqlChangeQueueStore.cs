using LiveSync.Application.Configuration;
using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LiveSync.Infrastructure.Integration.ChangeQueue;

public sealed class SqlChangeQueueStore(
    AppDbContext db,
    IOptions<ChangeDetectionSettings> settings) : IChangeQueueStore
{
    public async Task EnqueueAsync(ChangeEnvelope change, CancellationToken ct = default)
    {
        db.ChangeQueue.Add(new ChangeQueueEntry
        {
            Key = change.Key,
            Payload = change.Payload,
            Version = settings.Value.QueueVersion,
            CreatedAt = change.CreatedAt ?? DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<QueuedChange>> ClaimBatchAsync(string version, int batchSize, CancellationToken ct = default)
    {
        var maxRetries = settings.Value.MaxRetries;
        var claimToken = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var staleBefore = now.AddSeconds(-settings.Value.ClaimStaleAfterSeconds);

        await db.ChangeQueue
            .Where(x => x.ProcessedAt == null && x.ClaimedAt != null && x.ClaimedAt < staleBefore)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.ClaimToken, (string?)null), ct);

        var candidateIds = await db.Database
            .SqlQuery<long>($"""
                SELECT Id
                FROM ChangeQueue WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Version = {version}
                  AND ProcessedAt IS NULL
                  AND ClaimedAt IS NULL
                  AND RetryCount < {maxRetries}
                ORDER BY CreatedAt
                OFFSET 0 ROWS FETCH NEXT {batchSize} ROWS ONLY
                """)
            .ToListAsync(ct);

        if (candidateIds.Count == 0)
            return [];

        var rows = await db.ChangeQueue
            .Where(x => candidateIds.Contains(x.Id))
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.ClaimedAt = now;
            row.ClaimToken = claimToken;
        }

        await db.SaveChangesAsync(ct);

        return rows.Select(x => new QueuedChange
        {
            QueueEntryId = x.Id,
            ClaimToken = claimToken,
            Envelope = ChangeEnvelope.Parse(x.Key, x.Payload, x.CreatedAt)
        }).ToList();
    }

    public async Task MarkProcessedAsync(long queueEntryId, CancellationToken ct = default)
    {
        var row = await db.ChangeQueue.FindAsync([queueEntryId], ct);
        if (row is null)
            return;

        db.ChangeQueue.Remove(row);
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(long queueEntryId, string error, CancellationToken ct = default)
    {
        var row = await db.ChangeQueue.FindAsync([queueEntryId], ct);
        if (row is null)
            return;

        row.RetryCount++;
        row.LastError = error.Length > 2000 ? error[..2000] : error;
        row.ClaimedAt = null;
        row.ClaimToken = null;

        if (row.RetryCount >= settings.Value.MaxRetries)
            row.ProcessedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}

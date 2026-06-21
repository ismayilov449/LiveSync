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

        var rows = await db.ChangeQueue
            .Where(x => x.Version == version && x.ProcessedAt == null && x.RetryCount < maxRetries)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        return rows.Select(x => new QueuedChange
        {
            QueueEntryId = x.Id,
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

        if (row.RetryCount >= settings.Value.MaxRetries)
            row.ProcessedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}

using System.Diagnostics;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using LiveSync.Application.Observability;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSync.Application.CQRS.RealTimeSync.Commands;

public sealed class ProcessPendingChangesCommandHandler(
    IChangeQueueStore queue,
    ChangeProcessor processor,
    IDistributedLockFactory lockFactory,
    ITenantContext tenantContext,
    IOptions<ChangeDetectionSettings> settings,
    ILogger<ProcessPendingChangesCommandHandler> logger)
    : ICommandHandler<ProcessPendingChangesCommand>
{
    public async Task Handle(ProcessPendingChangesCommand request, CancellationToken ct)
    {
        var lockExpiry = TimeSpan.FromSeconds(settings.Value.LockExpirySeconds);
        var lockName = $"{settings.Value.DistributedLockName}:tenant:{tenantContext.TenantId}";

        await using var lockHandle = await lockFactory.AcquireAsync(
            lockName,
            lockExpiry,
            ct);

        if (!lockHandle.IsAcquired)
            return;

        var batch = await queue.ClaimBatchAsync(
            settings.Value.QueueVersion,
            settings.Value.BatchSize,
            ct);

        if (batch.Count == 0)
            return;

        logger.LogInformation("Processing {Count} pending change(s)", batch.Count);

        foreach (var queued in batch)
        {
            await lockHandle.RenewAsync(lockExpiry, ct);

            try
            {
                var sw = Stopwatch.StartNew();
                await processor.ProcessAsync(queued.Envelope, ct);
                sw.Stop();
                LiveSyncMetrics.ChangeProcessingDuration.Record(sw.Elapsed.TotalMilliseconds);
                await queue.MarkProcessedAsync(queued.QueueEntryId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process change. Id={Id}", queued.QueueEntryId);
                await queue.MarkFailedAsync(queued.QueueEntryId, ex.Message, ct);
            }
        }
    }
}

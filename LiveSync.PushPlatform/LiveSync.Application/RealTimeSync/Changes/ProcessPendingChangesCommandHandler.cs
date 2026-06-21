using LiveSync.Application.Configuration;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSync.Application.RealTimeSync.Changes;

public sealed class ProcessPendingChangesCommandHandler(
    IChangeQueueStore queue,
    ChangeProcessor processor,
    IDistributedLockFactory lockFactory,
    IOptions<ChangeDetectionSettings> settings,
    ILogger<ProcessPendingChangesCommandHandler> logger)
    : IRequestHandler<ProcessPendingChangesCommand>
{
    public async Task Handle(ProcessPendingChangesCommand request, CancellationToken ct)
    {
        await using var lockHandle = await lockFactory.AcquireAsync(
            settings.Value.DistributedLockName,
            TimeSpan.FromSeconds(30),
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
            try
            {
                await processor.ProcessAsync(queued.Envelope, ct);
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

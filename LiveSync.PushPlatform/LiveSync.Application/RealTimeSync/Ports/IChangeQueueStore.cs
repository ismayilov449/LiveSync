using LiveSync.Application.RealTimeSync.Models;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface IChangeQueueStore
{
    Task EnqueueAsync(ChangeEnvelope change, CancellationToken ct = default);
    Task<IReadOnlyList<QueuedChange>> ClaimBatchAsync(string version, int batchSize, CancellationToken ct = default);
    Task MarkProcessedAsync(long queueEntryId, CancellationToken ct = default);
    Task MarkFailedAsync(long queueEntryId, string error, CancellationToken ct = default);
}

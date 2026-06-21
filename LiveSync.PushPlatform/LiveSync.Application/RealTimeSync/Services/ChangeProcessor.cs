using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.Services;

public sealed class ChangeProcessor(IEnumerable<IChangeHandler> handlers)
{
    private readonly IReadOnlyDictionary<TopicBucket, IChangeHandler> _handlers =
        handlers.ToDictionary(h => h.Bucket);

    public Task ProcessAsync(ChangeEnvelope change, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(change.Bucket, out var handler))
            throw new NotSupportedException($"No change handler registered for bucket '{change.Bucket}'.");

        return handler.HandleAsync(change, ct);
    }
}

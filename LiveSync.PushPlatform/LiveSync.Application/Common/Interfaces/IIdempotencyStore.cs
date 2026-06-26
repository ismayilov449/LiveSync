namespace LiveSync.Application.Common.Interfaces;

public sealed record IdempotencyResult(int ResourceId, bool WasReplayed);

public interface IIdempotencyStore
{
    Task<IdempotencyResult?> TryGetAsync(string key, CancellationToken ct = default);
    Task SaveAsync(string key, int resourceId, CancellationToken ct = default);
}

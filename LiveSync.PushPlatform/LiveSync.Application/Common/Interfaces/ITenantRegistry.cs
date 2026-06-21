namespace LiveSync.Application.Common.Interfaces;

public interface ITenantRegistry
{
    Task<IReadOnlyList<int>> GetActiveTenantIdsAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(int tenantId, CancellationToken ct = default);
}

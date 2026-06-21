namespace LiveSync.Application.Common.Interfaces;

public interface ITenantItemBootstrap
{
    Task<int> EnsureRootItemAsync(int tenantId, CancellationToken ct = default);
}

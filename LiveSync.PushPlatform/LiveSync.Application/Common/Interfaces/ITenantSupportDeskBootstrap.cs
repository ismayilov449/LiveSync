namespace LiveSync.Application.Common.Interfaces;

public interface ITenantSupportDeskBootstrap
{
    Task<int> EnsureDefaultQueueAsync(int tenantId, CancellationToken ct = default);
}

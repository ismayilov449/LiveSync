namespace LiveSync.Application.Common.Interfaces;

public interface ITenantLifecycleService
{
    Task SuspendAsync(int tenantId, CancellationToken ct = default);
    Task ReactivateAsync(int tenantId, CancellationToken ct = default);
}

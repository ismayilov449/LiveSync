namespace LiveSync.Application.Common.Interfaces;

public interface ITenantConnectionResolver
{
    Task<string> GetConnectionStringAsync(int tenantId, CancellationToken ct = default);
}

namespace LiveSync.Application.Common.Interfaces;

public sealed record ProvisionedTenant(int Id, string Name, string DatabaseName);

public interface ITenantProvisioner
{
    Task<ProvisionedTenant> ProvisionTenantAsync(string name, CancellationToken ct = default);
    Task EnsureTenantDatabaseAsync(int tenantId, CancellationToken ct = default);
}

using LiveSync.Application.Configuration;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantConnectionResolver(
    MasterDbContext masterDb,
    IMemoryCache cache,
    IOptions<TenancySettings> tenancyOptions) : ITenantConnectionResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<string> GetConnectionStringAsync(int tenantId, CancellationToken ct = default)
    {
        var cacheKey = $"tenant-connection:{tenantId}";

        if (cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var tenant = await masterDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new InvalidOperationException(
                $"Tenant {tenantId} was not found in the control plane. Register a tenant or run database initialization.");

        if (tenant.Status == TenantStatus.Suspended)
            throw new InvalidOperationException($"Tenant {tenantId} is suspended.");

        if (tenant.Status is not (TenantStatus.Active or TenantStatus.Provisioning))
        {
            throw new InvalidOperationException(
                $"Tenant {tenantId} is not available (status: {tenant.Status}).");
        }

        if (string.IsNullOrWhiteSpace(tenant.DatabaseName))
        {
            throw new InvalidOperationException(
                $"Tenant {tenantId} has no database assigned in the control plane.");
        }

        var connectionString = BuildConnectionString(tenant.DatabaseName);
        cache.Set(cacheKey, connectionString, CacheDuration);
        return connectionString;
    }

    internal string BuildConnectionString(string databaseName)
    {
        var settings = tenancyOptions.Value;
        return settings.ConnectionTemplate.Replace("{DatabaseName}", databaseName, StringComparison.Ordinal);
    }
}

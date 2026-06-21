using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantProvisioner(
    MasterDbContext masterDb,
    ITenantDbContextFactory tenantDbContextFactory,
    ITenantItemBootstrap tenantItemBootstrap,
    IOptions<TenancySettings> tenancyOptions,
    IConfiguration configuration,
    ILogger<TenantProvisioner> logger) : ITenantProvisioner
{
    public async Task<ProvisionedTenant> ProvisionTenantAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        var tenant = new Tenant
        {
            Name = name.Trim(),
            Status = TenantStatus.Provisioning
        };

        masterDb.Tenants.Add(tenant);
        await masterDb.SaveChangesAsync(ct);

        tenant.DatabaseName = $"{tenancyOptions.Value.DatabaseNamePrefix}{tenant.Id}";
        await masterDb.SaveChangesAsync(ct);

        await CreatePhysicalDatabaseAsync(tenant.DatabaseName, ct);
        await tenantDbContextFactory.MigrateTenantDatabaseAsync(tenant.Id, ct);
        await tenantItemBootstrap.EnsureRootItemAsync(tenant.Id, ct);

        tenant.Status = TenantStatus.Active;
        await masterDb.SaveChangesAsync(ct);

        logger.LogInformation(
            "Provisioned tenant {TenantId} with database {DatabaseName}",
            tenant.Id,
            tenant.DatabaseName);

        return new ProvisionedTenant(tenant.Id, tenant.Name, tenant.DatabaseName);
    }

    public async Task EnsureTenantDatabaseAsync(int tenantId, CancellationToken ct = default)
    {
        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found.");

        if (string.IsNullOrWhiteSpace(tenant.DatabaseName))
        {
            tenant.DatabaseName = $"{tenancyOptions.Value.DatabaseNamePrefix}{tenant.Id}";
            await masterDb.SaveChangesAsync(ct);
        }

        await CreatePhysicalDatabaseAsync(tenant.DatabaseName, ct);
        await tenantDbContextFactory.MigrateTenantDatabaseAsync(tenant.Id, ct);

        if (tenant.Status != TenantStatus.Active)
        {
            tenant.Status = TenantStatus.Active;
            await masterDb.SaveChangesAsync(ct);
        }
    }

    private async Task CreatePhysicalDatabaseAsync(string databaseName, CancellationToken ct)
    {
        var masterConnectionString = GetMasterServerConnectionString();

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF DB_ID(@databaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE [' + REPLACE(@databaseName, ']', ']]') + N']';
                EXEC sp_executesql @sql;
            END
            """;
        command.Parameters.AddWithValue("@databaseName", databaseName);
        await command.ExecuteNonQueryAsync(ct);
    }

    private string GetMasterServerConnectionString()
    {
        var controlPlane = configuration.GetConnectionString("ControlPlane")
            ?? throw new InvalidOperationException("Connection string 'ControlPlane' is not configured.");

        var builder = new SqlConnectionStringBuilder(controlPlane)
        {
            InitialCatalog = "master"
        };

        return builder.ConnectionString;
    }
}

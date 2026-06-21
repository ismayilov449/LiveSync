using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Tenancy;

public interface ITenantDbContextFactory
{
    AppDbContext CreateDbContext(int tenantId, ITenantContext tenantContext);
    AppDbContext CreateMigrationDbContext(int tenantId);
    Task MigrateTenantDatabaseAsync(int tenantId, CancellationToken ct = default);
    Task EnsureDatabaseExistsAsync(int tenantId, CancellationToken ct = default);
}

public sealed class TenantDbContextFactory(ITenantConnectionResolver connectionResolver) : ITenantDbContextFactory
{
    public AppDbContext CreateDbContext(int tenantId, ITenantContext tenantContext)
    {
        if (!tenantContext.IsSet)
            tenantContext.SetTenantId(tenantId);
        else if (tenantContext.TenantId != tenantId)
        {
            throw new InvalidOperationException(
                $"Tenant context is set to {tenantContext.TenantId} but {tenantId} was requested.");
        }

        var connectionString = connectionResolver.GetConnectionStringAsync(tenantId).GetAwaiter().GetResult();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new AppDbContext(optionsBuilder.Options, tenantContext);
    }

    public AppDbContext CreateMigrationDbContext(int tenantId)
    {
        var connectionString = connectionResolver.GetConnectionStringAsync(tenantId).GetAwaiter().GetResult();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }

    public async Task EnsureDatabaseExistsAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await CreateMigrationDbContextAsync(tenantId, ct);
        await db.Database.EnsureCreatedAsync(ct);
    }

    public async Task MigrateTenantDatabaseAsync(int tenantId, CancellationToken ct = default)
    {
        await using var db = await CreateMigrationDbContextAsync(tenantId, ct);
        await db.Database.MigrateAsync(ct);
    }

    private async Task<AppDbContext> CreateMigrationDbContextAsync(int tenantId, CancellationToken ct)
    {
        var connectionString = await connectionResolver.GetConnectionStringAsync(tenantId, ct);
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}

using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Configuration;
using LiveSync.Infrastructure.Identity;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using LiveSync.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiveSync.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var hostingOptions = services.GetRequiredService<InfrastructureHostingOptions>();
        if (!hostingOptions.ApplyControlPlaneMigrationsOnStartup
            && !hostingOptions.MigrateFromSharedDatabaseOnStartup
            && !hostingOptions.MigrateTenantDatabasesOnStartup
            && !hostingOptions.SeedDataOnStartup)
            return;

        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        if (hostingOptions.ApplyControlPlaneMigrationsOnStartup)
        {
            logger.LogInformation("Applying control plane migrations...");
            await EnsureDatabaseExistsAsync(masterDb, ct);
            await masterDb.Database.MigrateAsync(ct);
        }

        if (hostingOptions.ApplyControlPlaneMigrationsOnStartup
            || hostingOptions.MigrateTenantDatabasesOnStartup
            || hostingOptions.SeedDataOnStartup)
        {
            await IdentityRoleSeeder.EnsureRolesAsync(masterDb, ct);
        }

        if (hostingOptions.MigrateFromSharedDatabaseOnStartup)
        {
            var migrator = scope.ServiceProvider.GetRequiredService<ISharedDatabaseMigrator>();
            logger.LogInformation("Migrating data from legacy shared database...");
            await migrator.MigrateAsync(ct);
        }

        if (hostingOptions.MigrateTenantDatabasesOnStartup)
            await MigrateAllTenantDatabasesAsync(scope.ServiceProvider, logger, ct);

        await BootstrapTenantRootItemsAsync(scope.ServiceProvider, logger, ct);

        await RepairProvisioningTenantsAsync(scope.ServiceProvider, logger, ct);

        if (hostingOptions.SeedDataOnStartup)
            await SeedDevelopmentDataAsync(scope.ServiceProvider, logger, ct);
    }

    private static async Task BootstrapTenantRootItemsAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken ct)
    {
        var tenantRegistry = services.GetRequiredService<ITenantRegistry>();
        var tenantItemBootstrap = services.GetRequiredService<ITenantItemBootstrap>();
        var tenantIds = await tenantRegistry.GetActiveTenantIdsAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            var rootId = await tenantItemBootstrap.EnsureRootItemAsync(tenantId, ct);
            logger.LogInformation(
                "Ensured root item {RootItemId} exists for tenant {TenantId}",
                rootId,
                tenantId);
        }
    }

    private static async Task RepairProvisioningTenantsAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken ct)
    {
        var masterDb = services.GetRequiredService<MasterDbContext>();
        var tenantProvisioner = services.GetRequiredService<ITenantProvisioner>();

        var provisioningTenantIds = await masterDb.Tenants
            .Where(x => x.Status == TenantStatus.Provisioning)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var tenantId in provisioningTenantIds)
        {
            logger.LogWarning(
                "Repairing tenant {TenantId} left in Provisioning state",
                tenantId);

            await tenantProvisioner.EnsureTenantDatabaseAsync(tenantId, ct);
        }
    }

    private static async Task MigrateAllTenantDatabasesAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken ct)
    {
        var tenantRegistry = services.GetRequiredService<ITenantRegistry>();
        var tenantDbContextFactory = services.GetRequiredService<ITenantDbContextFactory>();
        var tenantIds = await tenantRegistry.GetActiveTenantIdsAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            logger.LogInformation("Applying tenant database migrations for tenant {TenantId}", tenantId);
            await tenantDbContextFactory.MigrateTenantDatabaseAsync(tenantId, ct);
        }
    }

    private static async Task SeedDevelopmentDataAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken ct)
    {
        var masterDb = services.GetRequiredService<MasterDbContext>();
        var tenantProvisioner = services.GetRequiredService<ITenantProvisioner>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await masterDb.Tenants.AnyAsync(ct))
        {
            logger.LogInformation("Provisioning default development tenant...");
            await tenantProvisioner.ProvisionTenantAsync("Default Tenant", ct);
        }

        var tenant = await masterDb.Tenants.OrderBy(x => x.Id).FirstAsync(ct);

        if (await userManager.Users.AnyAsync(ct))
            return;

        var user = new ApplicationUser
        {
            UserName = "admin@livesync.local",
            Email = "admin@livesync.local",
            EmailConfirmed = true,
            DisplayName = "Admin",
            TenantId = tenant.Id
        };

        var result = await userManager.CreateAsync(user, "Admin123!");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed development user: {errors}");
        }

        await IdentityRoleSeeder.AssignTenantAdminAsync(userManager, user, ct);

        logger.LogInformation(
            "Seeded development user {UserName} for tenant {TenantId}",
            user.UserName,
            tenant.Id);

        await SeedTenantDatabaseAsync(services, tenant.Id, logger, ct);
    }

    private static async Task SeedTenantDatabaseAsync(
        IServiceProvider services,
        int tenantId,
        ILogger logger,
        CancellationToken ct)
    {
        var tenantItemBootstrap = services.GetRequiredService<ITenantItemBootstrap>();
        var rootId = await tenantItemBootstrap.EnsureRootItemAsync(tenantId, ct);

        var tenantContext = services.GetRequiredService<ITenantContext>();
        tenantContext.SetTenantId(tenantId);

        var tenantDb = services.GetRequiredService<AppDbContext>();
        if (await tenantDb.Items.CountAsync(ct) > 1)
            return;

        logger.LogInformation("Seeding sample items for tenant {TenantId}", tenantId);

        tenantDb.Items.Add(Domain.Entities.ItemAggregate.Item.Create(tenantId, rootId, "Apple"));
        tenantDb.Items.Add(Domain.Entities.ItemAggregate.Item.Create(tenantId, rootId, "Banana"));
        tenantDb.Items.Add(Domain.Entities.ItemAggregate.Item.Create(tenantId, rootId, "Cherry"));
        await tenantDb.SaveChangesAsync(ct);
    }

    private static async Task EnsureDatabaseExistsAsync(MasterDbContext masterDb, CancellationToken ct)
    {
        var connectionString = masterDb.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Control plane connection string is not configured.");

        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            return;

        builder.InitialCatalog = "master";
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
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
}

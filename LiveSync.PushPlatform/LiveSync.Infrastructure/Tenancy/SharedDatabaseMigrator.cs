using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using LiveSync.Infrastructure.Persistence.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class SharedDatabaseMigrator(
    MasterDbContext masterDb,
    ITenantProvisioner tenantProvisioner,
    ITenantDbContextFactory tenantDbContextFactory,
    IOptions<TenancySettings> tenancyOptions,
    IConfiguration configuration,
    ILogger<SharedDatabaseMigrator> logger) : ISharedDatabaseMigrator
{
    public async Task MigrateAsync(CancellationToken ct = default)
    {
        var settings = tenancyOptions.Value;
        if (!settings.MigrateFromSharedDatabase)
            return;

        var legacyConnectionString = BuildLegacyConnectionString(settings);
        if (!await LegacyDatabaseExistsAsync(legacyConnectionString, ct))
        {
            logger.LogInformation("Legacy database {DatabaseName} was not found. Skipping shared DB migration.",
                settings.LegacyDatabaseName);
            return;
        }

        var tenantIds = await ReadLegacyTenantIdsAsync(legacyConnectionString, ct);
        if (tenantIds.Count == 0)
        {
            logger.LogInformation("Legacy database has no tenant data. Skipping shared DB migration.");
            return;
        }

        logger.LogInformation(
            "Migrating {TenantCount} tenant(s) from legacy database {DatabaseName}",
            tenantIds.Count,
            settings.LegacyDatabaseName);

        foreach (var tenantId in tenantIds)
        {
            await MigrateTenantAsync(tenantId, legacyConnectionString, ct);
        }
    }

    private async Task MigrateTenantAsync(int tenantId, string legacyConnectionString, CancellationToken ct)
    {
        var tenant = await masterDb.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct);
        if (tenant is null)
        {
            await InsertTenantRecordAsync(tenantId, $"Tenant {tenantId}", ct);
            tenant = await masterDb.Tenants.FirstAsync(x => x.Id == tenantId, ct);
        }

        await tenantProvisioner.EnsureTenantDatabaseAsync(tenantId, ct);

        await using var targetDb = tenantDbContextFactory.CreateMigrationDbContext(tenantId);

        var legacyItems = await ReadLegacyItemsAsync(legacyConnectionString, tenantId, ct);
        var legacyQueue = await ReadLegacyChangeQueueAsync(legacyConnectionString, tenantId, ct);

        var itemsCopied = await CopyMissingItemsAsync(targetDb, legacyItems, ct);
        var queueCopied = await CopyMissingChangeQueueEntriesAsync(targetDb, legacyQueue, ct);

        if (itemsCopied == 0 && queueCopied == 0)
        {
            logger.LogInformation(
                "Tenant {TenantId} legacy data is already present. Skipping copy.",
                tenantId);
            return;
        }

        logger.LogInformation(
            "Migrated tenant {TenantId}: {ItemCount} item(s), {QueueCount} queue entry(ies)",
            tenantId,
            itemsCopied,
            queueCopied);
    }

    private async Task InsertTenantRecordAsync(int tenantId, string name, CancellationToken ct)
    {
        var databaseName = $"{tenancyOptions.Value.DatabaseNamePrefix}{tenantId}";
        var createdAt = DateTime.UtcNow;

        await masterDb.Database.ExecuteSqlRawAsync(
            """
            SET IDENTITY_INSERT Tenants ON;
            INSERT INTO Tenants (Id, Name, DatabaseName, Status, CreatedAtUtc)
            VALUES ({0}, {1}, {2}, {3}, {4});
            SET IDENTITY_INSERT Tenants OFF;
            """,
            tenantId,
            name,
            databaseName,
            (int)TenantStatus.Provisioning,
            createdAt);
    }

    private string BuildLegacyConnectionString(TenancySettings settings)
    {
        var legacy = configuration.GetConnectionString("Legacy");
        if (!string.IsNullOrWhiteSpace(legacy))
            return legacy;

        return settings.ConnectionTemplate.Replace("{DatabaseName}", settings.LegacyDatabaseName, StringComparison.Ordinal);
    }

    private static async Task<bool> LegacyDatabaseExistsAsync(string connectionString, CancellationToken ct)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private static async Task<IReadOnlyList<int>> ReadLegacyTenantIdsAsync(
        string legacyConnectionString,
        CancellationToken ct)
    {
        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT TenantId FROM Items ORDER BY TenantId";

        var tenantIds = new List<int>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            tenantIds.Add(reader.GetInt32(0));

        return tenantIds;
    }

    private static async Task<List<Domain.Entities.ItemAggregate.Item>> ReadLegacyItemsAsync(
        string legacyConnectionString,
        int tenantId,
        CancellationToken ct)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(legacyConnectionString);

        await using var legacyDb = new AppDbContext(optionsBuilder.Options);
        return await legacyDb.Items
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(ct);
    }

    private static async Task<List<ChangeQueueEntry>> ReadLegacyChangeQueueAsync(
        string legacyConnectionString,
        int tenantId,
        CancellationToken ct)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(legacyConnectionString);

        await using var legacyDb = new AppDbContext(optionsBuilder.Options);
        var tenantToken = $"tenantId#{tenantId}";
        return await legacyDb.ChangeQueue
            .AsNoTracking()
            .Where(x => x.Key.Contains(tenantToken))
            .ToListAsync(ct);
    }

    private static async Task<int> CopyMissingItemsAsync(
        AppDbContext targetDb,
        IReadOnlyList<Domain.Entities.ItemAggregate.Item> legacyItems,
        CancellationToken ct)
    {
        if (legacyItems.Count == 0)
            return 0;

        var existingIds = await targetDb.Items
            .Select(x => x.Id)
            .ToListAsync(ct);

        var existingIdSet = existingIds.ToHashSet();
        var itemsToCopy = legacyItems
            .Where(x => !existingIdSet.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ToList();

        if (itemsToCopy.Count == 0)
            return 0;

        await using var transaction = await targetDb.Database.BeginTransactionAsync(ct);
        await targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Items ON", ct);

        targetDb.Items.AddRange(itemsToCopy);
        await targetDb.SaveChangesAsync(ct);

        await targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Items OFF", ct);

        var maxId = await targetDb.Items.MaxAsync(x => (int?)x.Id, ct) ?? 0;
        await targetDb.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Items', RESEED, {0})", maxId, ct);

        await transaction.CommitAsync(ct);
        return itemsToCopy.Count;
    }

    private static async Task<int> CopyMissingChangeQueueEntriesAsync(
        AppDbContext targetDb,
        IReadOnlyList<ChangeQueueEntry> legacyQueue,
        CancellationToken ct)
    {
        if (legacyQueue.Count == 0)
            return 0;

        var existingKeys = await targetDb.ChangeQueue
            .Select(x => x.Key)
            .ToListAsync(ct);

        var existingKeySet = existingKeys.ToHashSet(StringComparer.Ordinal);
        var entriesToCopy = legacyQueue
            .Where(x => !existingKeySet.Contains(x.Key))
            .ToList();

        if (entriesToCopy.Count == 0)
            return 0;

        foreach (var entry in entriesToCopy)
            entry.Id = 0;

        targetDb.ChangeQueue.AddRange(entriesToCopy);
        await targetDb.SaveChangesAsync(ct);
        return entriesToCopy.Count;
    }
}

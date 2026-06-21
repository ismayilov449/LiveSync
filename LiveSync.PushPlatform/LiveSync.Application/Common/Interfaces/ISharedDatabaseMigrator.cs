namespace LiveSync.Application.Common.Interfaces;

public interface ISharedDatabaseMigrator
{
    Task MigrateAsync(CancellationToken ct = default);
}

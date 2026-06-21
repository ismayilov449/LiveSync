namespace LiveSync.Infrastructure.Configuration;

public sealed class InfrastructureHostingOptions
{
    public bool RunChangeDetection { get; set; } = true;
    public bool RunSubscriptionExpiry { get; set; } = true;
    public bool ApplyControlPlaneMigrationsOnStartup { get; set; }
    public bool MigrateFromSharedDatabaseOnStartup { get; set; }
    public bool MigrateTenantDatabasesOnStartup { get; set; }
    public bool SeedDataOnStartup { get; set; }
}
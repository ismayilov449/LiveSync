namespace LiveSync.Application.Configuration;

public sealed class TenancySettings
{
    public const string SectionName = "Tenancy";

    public string DatabaseNamePrefix { get; set; } = "LiveSync_Tenant_";
    public string ConnectionTemplate { get; set; } =
        "Server=localhost,1433;Database={DatabaseName};User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

    public bool MigrateFromSharedDatabase { get; set; }
    public string LegacyDatabaseName { get; set; } = "LiveSyncDb";
    public string ControlPlaneDatabaseName { get; set; } = "LiveSync_ControlPlane";
}

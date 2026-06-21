namespace LiveSync.Infrastructure.Persistence.ControlPlane;

public sealed class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

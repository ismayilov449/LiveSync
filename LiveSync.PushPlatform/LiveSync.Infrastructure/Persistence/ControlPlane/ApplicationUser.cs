using Microsoft.AspNetCore.Identity;

namespace LiveSync.Infrastructure.Persistence.ControlPlane;

public sealed class ApplicationUser : IdentityUser<int>
{
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

using System.Security.Claims;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;

namespace LiveSync.API.Identity;

public sealed class DevelopmentIdentityUser : IIdentityUser
{
    public DevelopmentIdentityUser(DevelopmentAuthSettings settings)
    {
        TenantId = settings.DefaultTenantId;
        UserId = settings.DefaultUserId;
        UserName = settings.DefaultUserName;
    }

    public DevelopmentIdentityUser(int tenantId, int userId, string userName)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
    }

    public ClaimsPrincipal? Claims { get; } = null;
    public int TenantId { get; }
    public int UserId { get; }
    public string UserName { get; }
    public bool IsAuthenticated { get; } = false;
}

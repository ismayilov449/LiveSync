using System.Security.Claims;

namespace LiveSync.Application.Common.Interfaces;

public interface IIdentityUser
{
    ClaimsPrincipal? Claims { get; }
    int TenantId { get; }
    int UserId { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
}

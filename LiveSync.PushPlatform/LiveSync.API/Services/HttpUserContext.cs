using LiveSync.Application.Common.Interfaces;

namespace LiveSync.API.Services;

public sealed class HttpUserContext(IIdentityUserAccessor identityUserAccessor) : IUserContext
{
    private IIdentityUser User => identityUserAccessor.Current;

    public System.Security.Claims.ClaimsPrincipal? Claims => User.Claims;
    public int TenantId => User.TenantId;
    public int UserId => User.UserId;
    public string UserName => User.UserName;
    public bool IsAuthenticated => User.IsAuthenticated;
}

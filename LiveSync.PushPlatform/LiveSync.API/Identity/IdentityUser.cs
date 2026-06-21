using System.Security.Claims;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;

namespace LiveSync.API.Identity;

public sealed class IdentityUser : IIdentityUser
{
    private readonly AccessTokenClaims? _token;

    public IdentityUser(ClaimsPrincipal claims, ClaimSettings claimSettings)
    {
        Claims = claims;

        if (claims.Identity?.IsAuthenticated == true && claims.Claims.Any())
            _token = AccessTokenClaims.TryParse(claims.Claims, claimSettings);
    }

    public ClaimsPrincipal? Claims { get; }

    public bool IsAuthenticated => _token is not null;

    public int TenantId => _token?.TenantId
        ?? throw new UnauthorizedAccessException("Tenant id claim is missing or invalid.");

    public int UserId => _token?.UserId
        ?? throw new UnauthorizedAccessException("User id claim is missing or invalid.");

    public string UserName => _token?.UserName ?? string.Empty;
}

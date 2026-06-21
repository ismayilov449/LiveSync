using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using Microsoft.Extensions.Options;

namespace LiveSync.API.Identity;

public sealed class HttpIdentityUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthSettings> authOptions,
    IHostEnvironment environment) : IIdentityUserAccessor
{
    public IIdentityUser Current => Resolve();

    private IIdentityUser Resolve()
    {
        var context = httpContextAccessor.HttpContext;
        var settings = authOptions.Value;

        if (context?.User.Identity?.IsAuthenticated == true)
        {
            var identityUser = new IdentityUser(context.User, settings.Claims);
            if (identityUser.IsAuthenticated)
                return identityUser;
        }

        if (environment.IsDevelopment()
            && settings.Development.AllowHeaderFallback
            && TryResolveFromHeaders(context, out var headerUser))
            return headerUser;

        if (!settings.RequireAuthentication
            || (environment.IsDevelopment() && settings.Development.AllowAnonymous))
            return new DevelopmentIdentityUser(settings.Development);

        throw new UnauthorizedAccessException(
            "Authentication is required. Provide a valid bearer token with tenant and user claims.");
    }

    private static bool TryResolveFromHeaders(HttpContext? context, out IIdentityUser user)
    {
        user = null!;

        if (context is null)
            return false;

        if (!TryReadPositiveInt(context, "X-Tenant-Id", "tenantId", out var tenantId))
            return false;

        if (!TryReadPositiveInt(context, "X-User-Id", "userId", out var userId))
            return false;

        context.Request.Headers.TryGetValue("X-User-Name", out var userNameHeader);
        var userName = userNameHeader.ToString();
        if (string.IsNullOrWhiteSpace(userName))
            userName = $"user-{userId}";

        user = new DevelopmentIdentityUser(tenantId, userId, userName);
        return true;
    }

    private static bool TryReadPositiveInt(
        HttpContext context,
        string headerName,
        string queryName,
        out int value)
    {
        value = 0;

        if (context.Request.Headers.TryGetValue(headerName, out var headerValue)
            && int.TryParse(headerValue.ToString(), out value)
            && value > 0)
            return true;

        if (context.Request.Query.TryGetValue(queryName, out var queryValue)
            && int.TryParse(queryValue.ToString(), out value)
            && value > 0)
            return true;

        return false;
    }
}

using System.Security.Claims;
using LiveSync.Application.Configuration;
using Microsoft.Extensions.Options;

namespace LiveSync.API.Middleware;

public sealed class DevelopmentHeaderAuthenticationMiddleware(
    RequestDelegate next,
    IOptions<AuthSettings> authOptions,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (environment.IsDevelopment()
            && authOptions.Value.Development.AllowHeaderFallback
            && context.User.Identity?.IsAuthenticated != true
            && TryCreatePrincipal(context, authOptions.Value.Claims, out var principal))
        {
            context.User = principal;
        }

        await next(context);
    }

    private static bool TryCreatePrincipal(
        HttpContext context,
        ClaimSettings claimSettings,
        out ClaimsPrincipal principal)
    {
        principal = null!;

        if (!TryReadPositiveInt(context, "X-Tenant-Id", "tenantId", out var tenantId))
            return false;

        if (!TryReadPositiveInt(context, "X-User-Id", "userId", out var userId))
            return false;

        context.Request.Headers.TryGetValue("X-User-Name", out var userNameHeader);
        var userName = userNameHeader.ToString();
        if (string.IsNullOrWhiteSpace(userName))
            userName = $"user-{userId}";

        var claims = new List<Claim>
        {
            new(claimSettings.TenantId, tenantId.ToString()),
            new(claimSettings.UserId, userId.ToString()),
            new(claimSettings.UserName, userName),
            new(ClaimTypes.Name, userName)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "DevelopmentHeader");
        principal = new ClaimsPrincipal(identity);
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

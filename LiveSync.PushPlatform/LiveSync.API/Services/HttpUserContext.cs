using LiveSync.Application.Common.Interfaces;

namespace LiveSync.API.Services;

public sealed class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public int TenantId => ReadInt("X-Tenant-Id", "tenantId", 1);

    public int UserId => ReadInt("X-User-Id", "userId", 1);

    private int ReadInt(string headerName, string queryName, int defaultValue)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return defaultValue;

        if (context.Request.Headers.TryGetValue(headerName, out var headerValue) &&
            int.TryParse(headerValue.ToString(), out var fromHeader))
            return fromHeader;

        if (context.Request.Query.TryGetValue(queryName, out var queryValue) &&
            int.TryParse(queryValue.ToString(), out var fromQuery))
            return fromQuery;

        return defaultValue;
    }
}

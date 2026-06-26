using System.Threading.RateLimiting;
using LiveSync.Application.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace LiveSync.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddLiveSyncRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(AuthPolicy, limiter =>
            {
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.PermitLimit = 30;
                limiter.QueueLimit = 0;
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var path = context.Request.Path.Value ?? string.Empty;

                if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
                    && path.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 30,
                            QueueLimit = 0
                        });
                }

                if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
                    && context.User.Identity?.IsAuthenticated == true)
                {
                    var settings = context.RequestServices
                        .GetRequiredService<IOptions<RateLimitSettings>>().Value;
                    var authSettings = context.RequestServices
                        .GetRequiredService<IOptions<AuthSettings>>().Value;
                    var tenantClaim = authSettings.Claims.TenantId;
                    var tenantId = context.User.FindFirst(tenantClaim)?.Value ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        tenantId,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromSeconds(settings.TenantWindowSeconds),
                            PermitLimit = settings.TenantPermitLimit,
                            QueueLimit = 0
                        });
                }

                return RateLimitPartition.GetNoLimiter("default");
            });
        });

        return services;
    }
}

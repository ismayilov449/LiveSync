using LiveSync.Application.Configuration;
using Microsoft.Extensions.Options;

namespace LiveSync.API.Middleware;

public sealed class ApiKeyMiddleware(RequestDelegate next, IOptions<AuthSettings> authOptions)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var configuredKey = authOptions.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey) ||
            !string.Equals(providedKey.ToString(), configuredKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { title = "Unauthorized", detail = "Invalid or missing API key." });
            return;
        }

        await next(context);
    }
}

using LiveSync.Application.Common.Interfaces;

namespace LiveSync.API.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserContext userContext,
        ITenantContext tenantContext,
        ITenantAccessValidator tenantAccessValidator)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var isDevelopmentHeaderAuth =
            context.User.Identity?.AuthenticationType == "DevelopmentHeader";

        if (!isDevelopmentHeaderAuth)
        {
            await tenantAccessValidator.EnsureUserBelongsToTenantAsync(
                userContext.UserId,
                userContext.TenantId,
                context.RequestAborted);
        }

        tenantContext.SetTenantId(userContext.TenantId);
        await next(context);
    }
}

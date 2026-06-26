using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.API.Middleware;

public sealed class TenantStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserContext userContext,
        MasterDbContext masterDb)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var isReactivate = path.Contains("/tenants/reactivate", StringComparison.OrdinalIgnoreCase);

            if (!isReactivate)
            {
                var status = await masterDb.Tenants
                    .AsNoTracking()
                    .Where(x => x.Id == userContext.TenantId)
                    .Select(x => x.Status)
                    .FirstOrDefaultAsync(context.RequestAborted);

                if (status == TenantStatus.Suspended)
                    throw new TenantSuspendedException(userContext.TenantId);
            }
        }

        await next(context);
    }
}

using Asp.Versioning;
using LiveSync.Application.Common.Constants;
using LiveSync.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSync.API.Controllers;

[Authorize(Roles = TenantRoles.TenantAdmin)]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants")]
[Route("api/tenants")]
public sealed class TenantsController(
    ITenantLifecycleService lifecycle,
    IUserContext userContext,
    IAuditService auditService) : ControllerBase
{
    [HttpPost("suspend")]
    public async Task<IActionResult> Suspend(CancellationToken ct)
    {
        await lifecycle.SuspendAsync(userContext.TenantId, ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "suspend",
            "tenant",
            userContext.TenantId.ToString(),
            "Tenant suspended by admin",
            ct);
        return NoContent();
    }

    [HttpPost("reactivate")]
    public async Task<IActionResult> Reactivate(CancellationToken ct)
    {
        await lifecycle.ReactivateAsync(userContext.TenantId, ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "reactivate",
            "tenant",
            userContext.TenantId.ToString(),
            "Tenant reactivated by admin",
            ct);
        return NoContent();
    }
}

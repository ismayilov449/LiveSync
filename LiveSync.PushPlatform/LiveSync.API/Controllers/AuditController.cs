using Asp.Versioning;
using LiveSync.Application.Common.Constants;
using LiveSync.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSync.API.Controllers;

[Authorize(Roles = TenantRoles.TenantAdmin)]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
[Route("api/audit")]
public sealed class AuditController(IAuditService auditService, IUserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedAuditEvents>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await auditService.ListAsync(userContext.TenantId, page, pageSize, ct);
        return Ok(result);
    }
}

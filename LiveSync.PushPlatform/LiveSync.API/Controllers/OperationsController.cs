using Asp.Versioning;
using LiveSync.Application.Common.Constants;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LiveSync.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/operations")]
[Route("api/operations")]
public sealed class OperationsController(
    IChangeQueueStore queue,
    IOptions<ChangeDetectionSettings> settings) : ControllerBase
{
    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpGet("change-queue")]
    public async Task<ActionResult<ChangeQueueStatsResponse>> GetChangeQueueStats(CancellationToken ct)
    {
        var stats = await queue.GetStatisticsAsync(settings.Value.QueueVersion, ct);
        return Ok(new ChangeQueueStatsResponse(stats.PendingCount, stats.DeadLetterCount));
    }
}

public sealed record ChangeQueueStatsResponse(int PendingCount, int DeadLetterCount);

using Asp.Versioning;
using LiveSync.API.Contracts.Queues;
using LiveSync.API.Mapping;
using LiveSync.Application.Common.Constants;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Queues.Commands;
using LiveSync.Application.CQRS.Queues.Models;
using LiveSync.Application.CQRS.Queues.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSync.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/queues")]
[Route("api/queues")]
public sealed class QueuesController(
    ISender sender,
    IUserContext userContext,
    IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedQueuesResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new ListQueuesQuery(userContext.TenantId, page, pageSize),
            ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QueueDto>> Get(int id, CancellationToken ct)
    {
        var queue = await sender.Send(new GetQueueByIdQuery(userContext.TenantId, id), ct);
        return queue is null ? NotFound() : Ok(queue);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateQueueRequest request,
        CancellationToken ct = default)
    {
        var id = await sender.Send(request.ToCommand(userContext.TenantId), ct);

        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "create",
            "queue",
            id.ToString(),
            request.Name,
            ct);

        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQueueRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "update",
            "queue",
            id.ToString(),
            request.Name,
            ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteQueueCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "delete",
            "queue",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await sender.Send(new DeactivateQueueCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "deactivate",
            "queue",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }
}

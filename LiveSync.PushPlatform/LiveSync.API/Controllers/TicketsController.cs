using Asp.Versioning;
using LiveSync.API.Contracts.Tickets;
using LiveSync.API.Mapping;
using LiveSync.Application.Common.Constants;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Tickets.Commands;
using LiveSync.Application.CQRS.Tickets.Models;
using LiveSync.Application.CQRS.Tickets.Queries;
using LiveSync.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSync.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
[Route("api/tickets")]
public sealed class TicketsController(
    ISender sender,
    IUserContext userContext,
    IIdempotencyStore idempotencyStore,
    IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedTicketsResponse>> List(
        [FromQuery] int? queueId,
        [FromQuery] TicketStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new ListTicketsQuery(userContext.TenantId, queueId, status, page, pageSize),
            ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDto>> Get(int id, CancellationToken ct)
    {
        var ticket = await sender.Send(new GetTicketByIdQuery(userContext.TenantId, id), ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Open(
        [FromBody] OpenTicketRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await idempotencyStore.TryGetAsync(idempotencyKey.Trim(), ct);
            if (existing is not null)
                return CreatedAtAction(nameof(Get), new { id = existing.ResourceId }, existing.ResourceId);
        }

        var id = await sender.Send(request.ToCommand(userContext.TenantId), ct);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            await idempotencyStore.SaveAsync(idempotencyKey.Trim(), id, ct);

        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "open",
            "ticket",
            id.ToString(),
            request.Subject,
            ct);

        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpPut("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignTicketRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "assign",
            "ticket",
            id.ToString(),
            $"assignee={request.AssigneeUserId}",
            ct);
        return NoContent();
    }

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "comment",
            "ticket",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }

    [HttpPost("{id:int}/start-progress")]
    public async Task<IActionResult> StartProgress(int id, CancellationToken ct)
    {
        await sender.Send(new StartProgressCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "start-progress",
            "ticket",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, CancellationToken ct)
    {
        await sender.Send(new ResolveTicketCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "resolve",
            "ticket",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id, CancellationToken ct)
    {
        await sender.Send(new CloseTicketCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "close",
            "ticket",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteTicketCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "delete",
            "ticket",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }
}

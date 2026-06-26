using Asp.Versioning;
using LiveSync.API.Contracts.Items;
using LiveSync.API.Mapping;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Commands;
using LiveSync.Application.CQRS.Items.Models;
using LiveSync.Application.CQRS.Items.Queries;
using LiveSync.Application.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSync.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/items")]
[Route("api/items")]
public sealed class ItemsController(
    ISender sender,
    IUserContext userContext,
    IIdempotencyStore idempotencyStore,
    IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedItemsResponse>> List(
        [FromQuery] int? parentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new ListItemsQuery(userContext.TenantId, parentId, page, pageSize),
            ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemDto>> Get(int id, CancellationToken ct)
    {
        var item = await sender.Send(new GetItemByIdQuery(userContext.TenantId, id), ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateItemRequest request,
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
            "create",
            "item",
            id.ToString(),
            request.Name,
            ct);

        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateItemRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "update",
            "item",
            id.ToString(),
            request.Name,
            ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteItemCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "delete",
            "item",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await sender.Send(new DeactivateItemCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "deactivate",
            "item",
            id.ToString(),
            null,
            ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpPut("{id:int}/parent")]
    public async Task<IActionResult> Move(int id, [FromBody] MoveItemRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(userContext.TenantId, id), ct);
        await auditService.RecordAsync(
            userContext.TenantId,
            userContext.UserId,
            "move",
            "item",
            id.ToString(),
            $"parent={request.ParentId}",
            ct);
        return NoContent();
    }
}

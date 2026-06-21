using Asp.Versioning;
using LiveSync.API.Contracts.Items;
using LiveSync.API.Mapping;
using LiveSync.Application.Common.Constants;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Commands;
using LiveSync.Application.CQRS.Items.Models;
using LiveSync.Application.CQRS.Items.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSync.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/items")]
[Route("api/items")]
public sealed class ItemsController(ISender sender, IUserContext userContext) : ControllerBase
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
    public async Task<ActionResult<int>> Create([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        var id = await sender.Send(request.ToCommand(userContext.TenantId), ct);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateItemRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(userContext.TenantId, id), ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteItemCommand(userContext.TenantId, id), ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await sender.Send(new DeactivateItemCommand(userContext.TenantId, id), ct);
        return NoContent();
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpPut("{id:int}/parent")]
    public async Task<IActionResult> Move(int id, [FromBody] MoveItemRequest request, CancellationToken ct)
    {
        await sender.Send(request.ToCommand(userContext.TenantId, id), ct);
        return NoContent();
    }
}

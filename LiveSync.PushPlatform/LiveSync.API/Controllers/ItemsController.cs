using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Items.Commands.CreateItem;
using LiveSync.Application.Items.Commands.DeactivateItem;
using LiveSync.Application.Items.Commands.DeleteItem;
using LiveSync.Application.Items.Commands.MoveItem;
using LiveSync.Application.Items.Commands.UpdateItem;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LiveSync.API.Controllers;

[ApiController]
[Route("api/items")]
public sealed class ItemsController(ISender sender, IUserContext userContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        var id = await sender.Send(
            new CreateItemCommand(userContext.TenantId, request.ParentId, request.Name),
            ct);
        return CreatedAtAction(nameof(Update), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateItemRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateItemCommand(userContext.TenantId, id, request.Name), ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteItemCommand(userContext.TenantId, id), ct);
        return NoContent();
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await sender.Send(new DeactivateItemCommand(userContext.TenantId, id), ct);
        return NoContent();
    }

    [HttpPut("{id:int}/parent")]
    public async Task<IActionResult> Move(int id, [FromBody] MoveItemRequest request, CancellationToken ct)
    {
        await sender.Send(new MoveItemCommand(userContext.TenantId, id, request.ParentId), ct);
        return NoContent();
    }
}

public sealed record CreateItemRequest(int ParentId, string Name);
public sealed record UpdateItemRequest(string Name);
public sealed record MoveItemRequest(int ParentId);

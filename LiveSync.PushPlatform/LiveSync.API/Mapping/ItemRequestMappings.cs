using LiveSync.API.Contracts.Items;
using LiveSync.Application.CQRS.Items.Commands;

namespace LiveSync.API.Mapping;

public static class ItemRequestMappings
{
    public static CreateItemCommand ToCommand(this CreateItemRequest request, int tenantId)
        => new(tenantId, request.ParentId, request.Name);

    public static UpdateItemCommand ToCommand(this UpdateItemRequest request, int tenantId, int id)
        => new(tenantId, id, request.Name);

    public static MoveItemCommand ToCommand(this MoveItemRequest request, int tenantId, int id)
        => new(tenantId, id, request.ParentId);
}

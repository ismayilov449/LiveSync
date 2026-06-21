using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Models;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Queries;

public sealed class GetItemByIdQueryHandler(IItemRepository itemRepository)
    : IQueryHandler<GetItemByIdQuery, ItemDto?>
{
    public async Task<ItemDto?> Handle(GetItemByIdQuery request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct);
        return item is null
            ? null
            : new ItemDto(
                item.Id,
                item.TenantId,
                item.ParentId,
                item.Name,
                item.IsActive,
                item.CreatedAtUtc);
    }
}

using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Models;
using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Queries;

public sealed class ListItemsQueryHandler(IItemRepository itemRepository)
    : IQueryHandler<ListItemsQuery, PagedItemsResponse>
{
    public async Task<PagedItemsResponse> Handle(ListItemsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        System.Linq.Expressions.Expression<Func<Item, bool>> predicate = request.ParentId.HasValue
            ? x => x.TenantId == request.TenantId && x.ParentId == request.ParentId.Value
            : x => x.TenantId == request.TenantId;

        var result = await itemRepository.ListPagedAsync(predicate, page, pageSize, ct);

        var items = result.Items
            .Select(item => new ItemDto(
                item.Id,
                item.TenantId,
                item.ParentId,
                item.Name,
                item.IsActive,
                item.CreatedAtUtc))
            .ToList();

        return new PagedItemsResponse(items, result.TotalCount, page, pageSize);
    }
}

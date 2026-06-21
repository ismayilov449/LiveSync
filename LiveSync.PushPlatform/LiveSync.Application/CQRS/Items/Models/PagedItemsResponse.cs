namespace LiveSync.Application.CQRS.Items.Models;

public sealed record PagedItemsResponse(
    IReadOnlyList<ItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

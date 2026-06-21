using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Models;

namespace LiveSync.Application.CQRS.Items.Queries;

public sealed record ListItemsQuery(
    int TenantId,
    int? ParentId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedItemsResponse>;

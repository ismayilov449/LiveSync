using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Queues.Models;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Queues.Queries;

public sealed class ListQueuesQueryHandler(IQueueRepository queueRepository)
    : IQueryHandler<ListQueuesQuery, PagedQueuesResponse>
{
    public async Task<PagedQueuesResponse> Handle(ListQueuesQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var result = await queueRepository.ListPagedAsync(
            x => x.TenantId == request.TenantId,
            page,
            pageSize,
            ct);

        var items = result.Items
            .Select(queue => new QueueDto(
                queue.Id,
                queue.TenantId,
                queue.Name,
                queue.IsActive,
                queue.CreatedAtUtc))
            .ToList();

        return new PagedQueuesResponse(items, result.TotalCount, page, pageSize);
    }
}

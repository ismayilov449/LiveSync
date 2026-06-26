using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Tickets.Models;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Tickets.Queries;

public sealed class ListTicketsQueryHandler(ITicketRepository ticketRepository)
    : IQueryHandler<ListTicketsQuery, PagedTicketsResponse>
{
    public async Task<PagedTicketsResponse> Handle(ListTicketsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var result = await ticketRepository.ListPagedAsync(
            x => x.TenantId == request.TenantId
                && (!request.QueueId.HasValue || x.QueueId == request.QueueId.Value)
                && (!request.Status.HasValue || x.Status == request.Status.Value),
            page,
            pageSize,
            ct);

        var items = result.Items.Select(t => TicketMapping.ToDto(t)).ToList();

        return new PagedTicketsResponse(items, result.TotalCount, page, pageSize);
    }
}

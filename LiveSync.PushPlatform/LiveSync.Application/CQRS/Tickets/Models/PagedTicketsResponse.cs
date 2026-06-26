namespace LiveSync.Application.CQRS.Tickets.Models;

public sealed record PagedTicketsResponse(
    IReadOnlyList<TicketDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

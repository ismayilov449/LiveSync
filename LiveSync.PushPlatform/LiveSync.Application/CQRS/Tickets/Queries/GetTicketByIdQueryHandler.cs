using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Tickets.Models;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Tickets.Queries;

public sealed class GetTicketByIdQueryHandler(ITicketRepository ticketRepository)
    : IQueryHandler<GetTicketByIdQuery, TicketDto?>
{
    public async Task<TicketDto?> Handle(GetTicketByIdQuery request, CancellationToken ct)
    {
        var ticket = await ticketRepository.GetByTenantAndIdWithCommentsAsync(request.TenantId, request.Id, ct);
        return ticket is null ? null : TicketMapping.ToDto(ticket, includeComments: true);
    }
}

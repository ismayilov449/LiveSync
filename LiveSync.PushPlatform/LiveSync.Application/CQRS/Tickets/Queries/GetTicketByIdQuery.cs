using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Tickets.Models;

namespace LiveSync.Application.CQRS.Tickets.Queries;

public sealed record GetTicketByIdQuery(int TenantId, int Id) : IQuery<TicketDto?>;

using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Tickets.Models;
using LiveSync.Domain.Entities.TicketAggregate;
using LiveSync.Domain.Enums;

namespace LiveSync.Application.CQRS.Tickets.Queries;

public sealed record ListTicketsQuery(
    int TenantId,
    int? QueueId,
    TicketStatus? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedTicketsResponse>;

internal static class TicketMapping
{
    public static TicketDto ToDto(Ticket ticket, bool includeComments = false)
    {
        var comments = includeComments
            ? ticket.Comments
                .Select(c => new TicketCommentDto(c.Id, c.AuthorUserId, c.Body, c.CreatedAtUtc))
                .ToList()
            : [];

        return new TicketDto(
            ticket.Id,
            ticket.TenantId,
            ticket.QueueId,
            ticket.Subject,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.ReporterUserId,
            ticket.AssigneeUserId,
            ticket.IsActive,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            comments);
    }
}

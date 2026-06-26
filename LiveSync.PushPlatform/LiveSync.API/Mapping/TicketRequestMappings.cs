using LiveSync.API.Contracts.Tickets;
using LiveSync.Application.CQRS.Tickets.Commands;

namespace LiveSync.API.Mapping;

public static class TicketRequestMappings
{
    public static OpenTicketCommand ToCommand(this OpenTicketRequest request, int tenantId)
        => new(
            tenantId,
            request.QueueId,
            request.Subject,
            request.Description,
            request.Priority,
            request.ReporterUserId);

    public static AssignTicketCommand ToCommand(this AssignTicketRequest request, int tenantId, int id)
        => new(tenantId, id, request.AssigneeUserId);

    public static AddCommentCommand ToCommand(this AddCommentRequest request, int tenantId, int id)
        => new(tenantId, id, request.AuthorUserId, request.Body);
}

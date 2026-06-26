using LiveSync.Domain.Enums;

namespace LiveSync.API.Contracts.Tickets;

public sealed record OpenTicketRequest(
    int QueueId,
    string Subject,
    string Description,
    TicketPriority Priority,
    int ReporterUserId);

public sealed record AssignTicketRequest(int AssigneeUserId);

public sealed record AddCommentRequest(int AuthorUserId, string Body);

namespace LiveSync.Application.CQRS.Tickets.Models;

public sealed record TicketCommentDto(
    int Id,
    int AuthorUserId,
    string Body,
    DateTime CreatedAtUtc);

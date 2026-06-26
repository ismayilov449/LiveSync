using LiveSync.Domain.Enums;

namespace LiveSync.Application.CQRS.Tickets.Models;

public sealed record TicketDto(
    int Id,
    int TenantId,
    int QueueId,
    string Subject,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    int ReporterUserId,
    int? AssigneeUserId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TicketCommentDto> Comments);

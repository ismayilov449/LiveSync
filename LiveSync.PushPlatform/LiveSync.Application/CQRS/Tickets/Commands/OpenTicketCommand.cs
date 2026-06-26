using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Enums;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed record OpenTicketCommand(
    int TenantId,
    int QueueId,
    string Subject,
    string Description,
    TicketPriority Priority,
    int ReporterUserId) : ICommand<int>;

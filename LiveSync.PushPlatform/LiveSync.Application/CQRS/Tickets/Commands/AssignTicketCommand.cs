using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed record AssignTicketCommand(int TenantId, int Id, int AssigneeUserId) : ICommand;

using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed record DeleteTicketCommand(int TenantId, int Id) : ICommand;

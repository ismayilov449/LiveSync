using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed record StartProgressCommand(int TenantId, int Id) : ICommand;

using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Queues.Commands;

public sealed record UpdateQueueCommand(int TenantId, int Id, string Name) : ICommand;

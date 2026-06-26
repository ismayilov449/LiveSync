using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Queues.Commands;

public sealed record DeactivateQueueCommand(int TenantId, int Id) : ICommand;

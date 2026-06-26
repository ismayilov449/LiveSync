using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Queues.Commands;

public sealed record CreateQueueCommand(int TenantId, string Name) : ICommand<int>;

using LiveSync.Application.CQRS.Queues.Commands;

namespace LiveSync.API.Contracts.Queues;

public sealed record CreateQueueRequest(string Name);

public sealed record UpdateQueueRequest(string Name);

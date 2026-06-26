using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Queues.Models;

namespace LiveSync.Application.CQRS.Queues.Queries;

public sealed record GetQueueByIdQuery(int TenantId, int Id) : IQuery<QueueDto?>;

using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Queues.Models;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Queues.Queries;

public sealed class GetQueueByIdQueryHandler(IQueueRepository queueRepository)
    : IQueryHandler<GetQueueByIdQuery, QueueDto?>
{
    public async Task<QueueDto?> Handle(GetQueueByIdQuery request, CancellationToken ct)
    {
        var queue = await queueRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct);
        if (queue is null)
            return null;

        return new QueueDto(
            queue.Id,
            queue.TenantId,
            queue.Name,
            queue.IsActive,
            queue.CreatedAtUtc);
    }
}

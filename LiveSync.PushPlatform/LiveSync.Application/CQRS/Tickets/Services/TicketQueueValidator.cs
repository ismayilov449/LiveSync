using LiveSync.Application.Common.Exceptions;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Tickets.Services;

public interface ITicketQueueValidator
{
    Task ValidateQueueAsync(int tenantId, int queueId, CancellationToken ct = default);
}

public sealed class TicketQueueValidator(IQueueRepository queueRepository) : ITicketQueueValidator
{
    public async Task ValidateQueueAsync(int tenantId, int queueId, CancellationToken ct = default)
    {
        if (queueId <= 0)
            throw new ConflictException("QueueId must be greater than zero.");

        var queue = await queueRepository.GetByTenantAndIdAsync(tenantId, queueId, ct)
            ?? throw new NotFoundException(
                $"Queue {queueId} was not found in your tenant. Queue IDs are per-tenant — choose an existing queue from the queues list.");

        if (!queue.IsActive)
            throw new ConflictException($"Queue {queueId} is not active.");
    }
}

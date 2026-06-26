using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Queues.Commands;

public sealed class DeactivateQueueCommandHandler(
    IQueueRepository queueRepository,
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeactivateQueueCommand>
{
    public async Task Handle(DeactivateQueueCommand request, CancellationToken ct)
    {
        var queue = await queueRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Queue with ID {request.Id} was not found.");

        var openCount = await ticketRepository.CountOpenInQueueAsync(request.TenantId, request.Id, ct);
        if (openCount > 0)
            throw new BusinessRuleException($"Cannot deactivate queue {request.Id} because it has {openCount} open ticket(s).");

        queue.Deactivate();

        await queueRepository.UpdateAsync(queue, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Queues.Commands;

public sealed class DeleteQueueCommandHandler(
    IQueueRepository queueRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteQueueCommand>
{
    public async Task Handle(DeleteQueueCommand request, CancellationToken ct)
    {
        var queue = await queueRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Queue with ID {request.Id} was not found.");

        queue.MarkDeleted();

        await queueRepository.DeleteAsync(queue, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

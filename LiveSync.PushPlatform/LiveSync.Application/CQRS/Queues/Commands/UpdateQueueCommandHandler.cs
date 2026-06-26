using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Queues.Commands;

public sealed class UpdateQueueCommandHandler(
    IQueueRepository queueRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateQueueCommand>
{
    public async Task Handle(UpdateQueueCommand request, CancellationToken ct)
    {
        var queue = await queueRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Queue with ID {request.Id} was not found.");

        queue.Rename(request.Name);

        await queueRepository.UpdateAsync(queue, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

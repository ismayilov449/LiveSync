using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Entities.QueueAggregate;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Queues.Commands;

public sealed class CreateQueueCommandHandler(
    IQueueRepository queueRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateQueueCommand, int>
{
    public async Task<int> Handle(CreateQueueCommand request, CancellationToken ct)
    {
        var queue = Queue.Create(request.TenantId, request.Name);

        await queueRepository.AddAsync(queue, ct);
        await unitOfWork.SaveChangesAsync(ct);

        queue.CompleteCreation();
        await unitOfWork.PublishDomainEventsAsync([queue], ct);

        return queue.Id;
    }
}

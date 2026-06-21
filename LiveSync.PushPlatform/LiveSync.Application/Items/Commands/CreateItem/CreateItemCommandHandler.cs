using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Domain.Entities.ItemAggregate.Events;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using MediatR;

namespace LiveSync.Application.Items.Commands.CreateItem;

public sealed class CreateItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork,
    IPublisher publisher) : IRequestHandler<CreateItemCommand, int>
{
    public async Task<int> Handle(CreateItemCommand request, CancellationToken ct)
    {
        var item = Item.Create(request.TenantId, request.ParentId, request.Name);

        await itemRepository.AddAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await publisher.Publish(new ItemCreatedDomainEvent(item.TenantId, item.Id), ct);

        return item.Id;
    }
}

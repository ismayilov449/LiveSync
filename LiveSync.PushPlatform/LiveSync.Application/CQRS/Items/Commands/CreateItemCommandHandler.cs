using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Services;
using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed class CreateItemCommandHandler(
    IItemRepository itemRepository,
    IItemHierarchyValidator hierarchyValidator,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateItemCommand, int>
{
    public async Task<int> Handle(CreateItemCommand request, CancellationToken ct)
    {
        await hierarchyValidator.ValidateParentAsync(request.TenantId, request.ParentId, itemId: null, ct);

        var item = Item.Create(request.TenantId, request.ParentId, request.Name);

        await itemRepository.AddAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);

        item.CompleteCreation();
        await unitOfWork.PublishDomainEventsAsync([item], ct);

        return item.Id;
    }
}

using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Services;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed class MoveItemCommandHandler(
    IItemRepository itemRepository,
    IItemHierarchyValidator hierarchyValidator,
    IUnitOfWork unitOfWork) : ICommandHandler<MoveItemCommand>
{
    public async Task Handle(MoveItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Item with ID {request.Id} was not found.");

        await hierarchyValidator.ValidateParentAsync(request.TenantId, request.ParentId, request.Id, ct);

        item.MoveToParent(request.ParentId);

        await itemRepository.UpdateAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using MediatR;

namespace LiveSync.Application.Items.Commands.MoveItem;

public sealed class MoveItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<MoveItemCommand>
{
    public async Task Handle(MoveItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new InvalidOperationException($"Item with ID {request.Id} not found.");

        item.MoveToParent(request.ParentId);

        await itemRepository.UpdateAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

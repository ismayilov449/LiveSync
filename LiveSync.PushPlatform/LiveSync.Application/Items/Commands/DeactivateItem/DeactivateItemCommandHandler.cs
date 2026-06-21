using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using MediatR;

namespace LiveSync.Application.Items.Commands.DeactivateItem;

public sealed class DeactivateItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateItemCommand>
{
    public async Task Handle(DeactivateItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new InvalidOperationException($"Item with ID {request.Id} not found.");

        item.Deactivate();

        await itemRepository.UpdateAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

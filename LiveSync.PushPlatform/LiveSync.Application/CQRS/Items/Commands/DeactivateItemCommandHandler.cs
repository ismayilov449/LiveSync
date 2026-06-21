using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed class DeactivateItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeactivateItemCommand>
{
    public async Task Handle(DeactivateItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Item with ID {request.Id} was not found.");

        item.Deactivate();

        await itemRepository.UpdateAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

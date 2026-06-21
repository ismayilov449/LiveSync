using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using MediatR;

namespace LiveSync.Application.Items.Commands.UpdateItem;

public sealed class UpdateItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateItemCommand>
{
    public async Task Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new InvalidOperationException($"Item with ID {request.Id} not found.");

        item.Rename(request.Name);

        await itemRepository.UpdateAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

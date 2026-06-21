using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed class UpdateItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateItemCommand>
{
    public async Task Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Item with ID {request.Id} was not found.");

        item.Rename(request.Name);

        await itemRepository.UpdateAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

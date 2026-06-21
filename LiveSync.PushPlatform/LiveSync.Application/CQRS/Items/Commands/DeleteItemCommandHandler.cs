using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed class DeleteItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteItemCommand>
{
    public async Task Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Item with ID {request.Id} was not found.");

        item.MarkDeleted();

        await itemRepository.DeleteAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

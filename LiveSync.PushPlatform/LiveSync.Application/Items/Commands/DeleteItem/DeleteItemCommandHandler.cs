using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using MediatR;

namespace LiveSync.Application.Items.Commands.DeleteItem;

public sealed class DeleteItemCommandHandler(
    IItemRepository itemRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteItemCommand>
{
    public async Task Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var item = await itemRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new InvalidOperationException($"Item with ID {request.Id} not found.");

        item.MarkDeleted();

        await itemRepository.DeleteAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

using LiveSync.Application.Common.Exceptions;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Items.Services;

public interface IItemHierarchyValidator
{
    Task ValidateParentAsync(int tenantId, int parentId, int? itemId, CancellationToken ct = default);
}

public sealed class ItemHierarchyValidator(IItemRepository itemRepository) : IItemHierarchyValidator
{
    public async Task ValidateParentAsync(int tenantId, int parentId, int? itemId, CancellationToken ct = default)
    {
        if (parentId <= 0)
            throw new ConflictException("ParentId must be greater than zero.");

        if (itemId.HasValue && parentId == itemId.Value)
            throw new ConflictException("An item cannot be its own parent.");

        var parent = await itemRepository.GetByTenantAndIdAsync(tenantId, parentId, ct)
            ?? throw new NotFoundException(
                $"Parent item {parentId} was not found in your tenant. Item IDs are per-tenant — choose an existing parent from the items list.");

        if (!parent.IsActive)
            throw new ConflictException($"Parent item {parentId} is not active.");

        if (!itemId.HasValue)
            return;

        if (await CreatesCycleAsync(tenantId, itemId.Value, parentId, ct))
            throw new ConflictException("Move would create a circular parent reference.");
    }

    private async Task<bool> CreatesCycleAsync(int tenantId, int itemId, int newParentId, CancellationToken ct)
    {
        var visited = new HashSet<int> { itemId };
        var currentId = newParentId;

        while (true)
        {
            if (!visited.Add(currentId))
                return true;

            var current = await itemRepository.GetByTenantAndIdAsync(tenantId, currentId, ct);
            if (current is null)
                return false;

            if (current.Id == itemId)
                return true;

            currentId = current.ParentId;
        }
    }
}

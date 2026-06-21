using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.CQRS.Items.Services;
using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Domain.Interfaces.Repositories;
using Moq;

namespace LiveSync.Tests;

public sealed class ItemHierarchyValidatorTests
{
    [Fact]
    public async Task ValidateParentAsync_WhenMoveCreatesCycle_ThrowsConflict()
    {
        var root = Item.Create(1, 1, "Root");
        SetId(root, 1);

        var child = Item.Create(1, 1, "Child");
        SetId(child, 2);

        var grandchild = Item.Create(1, 2, "Grandchild");
        SetId(grandchild, 3);

        var repository = new Mock<IItemRepository>();
        repository.Setup(r => r.GetByTenantAndIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(root);
        repository.Setup(r => r.GetByTenantAndIdAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        repository.Setup(r => r.GetByTenantAndIdAsync(1, 3, It.IsAny<CancellationToken>())).ReturnsAsync(grandchild);

        var validator = new ItemHierarchyValidator(repository.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            validator.ValidateParentAsync(1, parentId: 3, itemId: 1, CancellationToken.None));
    }

    private static void SetId(Item item, int id)
    {
        typeof(Item).GetProperty(nameof(Item.Id))!
            .SetValue(item, id);
    }
}

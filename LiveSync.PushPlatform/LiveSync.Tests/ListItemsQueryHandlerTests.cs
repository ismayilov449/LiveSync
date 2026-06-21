using LiveSync.Application.CQRS.Items.Models;
using LiveSync.Application.CQRS.Items.Queries;
using LiveSync.Domain.Entities.ItemAggregate;
using LiveSync.Domain.Interfaces.Repositories;
using Moq;

namespace LiveSync.Tests;

public sealed class ListItemsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedResponseOrderedByRepository()
    {
        var now = DateTime.UtcNow;
        var items = new List<Item>
        {
            CreateItem(2, 1, 1, "Newer", now),
            CreateItem(1, 1, 1, "Older", now.AddMinutes(-5)),
        };

        var repository = new Mock<IItemRepository>();
        repository
            .Setup(r => r.ListPagedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Item>(items, 2));

        var handler = new ListItemsQueryHandler(repository.Object);
        var result = await handler.Handle(new ListItemsQuery(TenantId: 1, Page: 1, PageSize: 20), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Newer", result.Items[0].Name);
        Assert.All(result.Items, dto => Assert.Equal(1, dto.TenantId));
    }

    [Fact]
    public async Task Handle_ClampsPageSizeToMaximum()
    {
        var repository = new Mock<IItemRepository>();
        repository
            .Setup(r => r.ListPagedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                1,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Item>([], 0));

        var handler = new ListItemsQueryHandler(repository.Object);
        await handler.Handle(new ListItemsQuery(1, PageSize: 500), CancellationToken.None);

        repository.Verify(r => r.ListPagedAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
            1,
            100,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Item CreateItem(int id, int tenantId, int parentId, string name, DateTime createdAtUtc)
    {
        var item = Item.Create(tenantId, parentId, name);
        typeof(Item).GetProperty(nameof(Item.Id))!.SetValue(item, id);
        typeof(Item).GetProperty(nameof(Item.CreatedAtUtc))!.SetValue(item, createdAtUtc);
        return item;
    }
}

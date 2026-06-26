using LiveSync.Application.CQRS.Queues.Commands;
using LiveSync.Application.CQRS.Queues.Models;
using LiveSync.Application.CQRS.Queues.Queries;
using LiveSync.Domain.Entities.QueueAggregate;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using Moq;

namespace LiveSync.Tests;

public sealed class CreateQueueCommandHandlerTests
{
  [Fact]
  public async Task Handle_PersistsQueueAndReturnsId()
  {
    Queue? captured = null;
    var repository = new Mock<IQueueRepository>();
    repository
      .Setup(r => r.AddAsync(It.IsAny<Queue>(), It.IsAny<CancellationToken>()))
      .Callback<Queue, CancellationToken>((queue, _) =>
      {
        captured = queue;
        typeof(Queue).GetProperty(nameof(Queue.Id))!.SetValue(queue, 42);
      })
      .Returns(Task.CompletedTask);

    var unitOfWork = new Mock<IUnitOfWork>();
    unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    unitOfWork
      .Setup(u => u.PublishDomainEventsAsync(It.IsAny<IEnumerable<Queue>>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var handler = new CreateQueueCommandHandler(repository.Object, unitOfWork.Object);
    var id = await handler.Handle(new CreateQueueCommand(1, "General"), CancellationToken.None);

    Assert.Equal(42, id);
    Assert.NotNull(captured);
    Assert.Equal("General", captured!.Name);
    Assert.Equal(1, captured.TenantId);
    repository.Verify(r => r.AddAsync(It.IsAny<Queue>(), It.IsAny<CancellationToken>()), Times.Once);
  }
}

public sealed class ListQueuesQueryHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsPagedQueuesForTenant()
  {
    var now = DateTime.UtcNow;
    var queues = new List<Queue>
    {
      CreateQueue(2, 1, "Newer", now),
      CreateQueue(1, 1, "Older", now.AddMinutes(-5)),
    };

    var repository = new Mock<IQueueRepository>();
    repository
      .Setup(r => r.ListPagedAsync(
        It.IsAny<System.Linq.Expressions.Expression<Func<Queue, bool>>>(),
        1,
        20,
        It.IsAny<CancellationToken>()))
      .ReturnsAsync(new PagedResult<Queue>(queues, 2));

    var handler = new ListQueuesQueryHandler(repository.Object);
    var result = await handler.Handle(new ListQueuesQuery(1, 1, 20), CancellationToken.None);

    Assert.Equal(2, result.TotalCount);
    Assert.Equal(2, result.Items.Count);
    Assert.Equal("Newer", result.Items[0].Name);
  }

  private static Queue CreateQueue(int id, int tenantId, string name, DateTime createdAtUtc)
  {
    var queue = Queue.Create(tenantId, name);
    typeof(Queue).GetProperty(nameof(Queue.Id))!.SetValue(queue, id);
    typeof(Queue).GetProperty(nameof(Queue.CreatedAtUtc))!.SetValue(queue, createdAtUtc);
    return queue;
  }
}

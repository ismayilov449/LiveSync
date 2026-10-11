using LiveSync.Application.Common.Behaviors;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.Services;
using LiveSync.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LiveSync.Tests;

public sealed class TransactionBehaviorTests
{
    private sealed record TestCommand : ICommand<int>, ITransactionalCommand;

    private static readonly ChangeNotificationDto TicketPush = new()
    {
        Entity = new ChangeEntityDto { Bucket = "ticket", Id = "ticket-1" }
    };

    private readonly Mock<IRealTimeNotifier> _notifier = new();
    private readonly List<string> _timeline = [];
    private readonly PostCommitNotifications _notifications;
    private readonly TransactionBehavior<TestCommand, int> _behavior;

    public TransactionBehaviorTests()
    {
        _notifier
            .Setup(n => n.NotifyBucketAsync(
                It.IsAny<int>(),
                It.IsAny<TopicBucket>(),
                It.IsAny<ChangeNotificationDto>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => _timeline.Add("push"))
            .Returns(Task.CompletedTask);

        _notifications = new PostCommitNotifications(_notifier.Object, NullLogger<PostCommitNotifications>.Instance);
        _behavior = new TransactionBehavior<TestCommand, int>(new RecordingTransactionRunner(_timeline), _notifications);
    }

    [Fact]
    public async Task Handle_PushesOnlyAfterCommit()
    {
        var result = await _behavior.Handle(new TestCommand(), () =>
        {
            _notifications.Add(1, TopicBucket.Ticket, TicketPush);
            return Task.FromResult(7);
        }, CancellationToken.None);

        Assert.Equal(7, result);
        Assert.Equal(["begin", "commit", "push"], _timeline);
    }

    [Fact]
    public async Task Handle_DoesNotPush_WhenHandlerFails()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(new TestCommand(), () =>
        {
            _notifications.Add(1, TopicBucket.Ticket, TicketPush);
            throw new InvalidOperationException("rolled back");
        }, CancellationToken.None));

        await _notifications.FlushAsync();
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Succeeds_WhenImmediatePushFails()
    {
        _notifier.Reset();
        _notifier
            .Setup(n => n.NotifyBucketAsync(
                It.IsAny<int>(),
                It.IsAny<TopicBucket>(),
                It.IsAny<ChangeNotificationDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("backplane down"));

        var result = await _behavior.Handle(new TestCommand(), () =>
        {
            _notifications.Add(1, TopicBucket.Ticket, TicketPush);
            return Task.FromResult(7);
        }, CancellationToken.None);

        Assert.Equal(7, result);
    }

    private sealed class RecordingTransactionRunner(List<string> timeline) : ITransactionRunner
    {
        public async Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken ct = default)
        {
            timeline.Add("begin");
            var result = await operation(ct);
            timeline.Add("commit");
            return result;
        }
    }
}

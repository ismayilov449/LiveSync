using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.Services;
using LiveSync.Domain.Enums;
using Moq;

namespace LiveSync.Tests;

public sealed class ChangeProcessorTests
{
    [Fact]
    public async Task ProcessAsync_RoutesToRegisteredBucketHandler()
    {
        var handler = new Mock<IChangeHandler>();
        handler.SetupGet(h => h.Bucket).Returns(TopicBucket.Item);

        var processor = new ChangeProcessor([handler.Object]);
        var envelope = new ChangeEnvelope
        {
            Key = "table#Item:tenantId#1:eventType#Update",
            Bucket = TopicBucket.Item,
            TenantId = 1,
            EventType = ChangeType.Update,
            EntityId = 10
        };

        await processor.ProcessAsync(envelope);

        handler.Verify(h => h.HandleAsync(envelope, It.IsAny<CancellationToken>()), Times.Once);
    }
}

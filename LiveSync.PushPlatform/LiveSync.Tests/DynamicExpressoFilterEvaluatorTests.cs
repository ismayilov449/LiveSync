using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Infrastructure.RealTimeSync;
using LiveSync.Infrastructure.RealTimeSync.Buckets;
using Microsoft.Extensions.Logging.Abstractions;

namespace LiveSync.Tests;

public sealed class DynamicExpressoFilterEvaluatorTests
{
    [Fact]
    public void IsValidFilter_ForTicketBucket_AcceptsValidExpression()
    {
        var registry = new BucketModuleRegistry([new TicketBucketModule(null!)]);
        var evaluator = new DynamicExpressoFilterEvaluator(registry, NullLogger<DynamicExpressoFilterEvaluator>.Instance);

        Assert.True(evaluator.IsValidFilter("ticket.QueueId == 5", TopicBucket.Ticket));
        Assert.False(evaluator.IsValidFilter("ticket.QueueId == ", TopicBucket.Ticket));
    }

    [Fact]
    public void Matches_ReturnsExpectedResult()
    {
        var registry = new BucketModuleRegistry([new TicketBucketModule(null!)]);
        var evaluator = new DynamicExpressoFilterEvaluator(registry, NullLogger<DynamicExpressoFilterEvaluator>.Instance);

        ICacheDto dto = new TicketCacheDto
        {
            Id = 1,
            TenantId = 1,
            QueueId = 5,
            Subject = "Login issue",
            IsActive = true
        };

        Assert.True(evaluator.Matches("ticket.QueueId == 5", dto));
        Assert.False(evaluator.Matches("ticket.QueueId == 99", dto));
    }
}

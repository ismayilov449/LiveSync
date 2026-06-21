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
    public void IsValidFilter_ForItemBucket_AcceptsValidExpression()
    {
        var registry = new BucketModuleRegistry([new ItemBucketModule(null!)]);
        var evaluator = new DynamicExpressoFilterEvaluator(registry, NullLogger<DynamicExpressoFilterEvaluator>.Instance);

        Assert.True(evaluator.IsValidFilter("item.ParentId == 5", TopicBucket.Item));
        Assert.False(evaluator.IsValidFilter("item.ParentId == ", TopicBucket.Item));
    }

    [Fact]
    public void Matches_ReturnsExpectedResult()
    {
        var registry = new BucketModuleRegistry([new ItemBucketModule(null!)]);
        var evaluator = new DynamicExpressoFilterEvaluator(registry, NullLogger<DynamicExpressoFilterEvaluator>.Instance);

        ICacheDto dto = new ItemCacheDto
        {
            Id = 1,
            TenantId = 1,
            ParentId = 5,
            Name = "Apple",
            IsActive = true
        };

        Assert.True(evaluator.Matches("item.ParentId == 5", dto));
        Assert.False(evaluator.Matches("item.ParentId == 99", dto));
    }
}

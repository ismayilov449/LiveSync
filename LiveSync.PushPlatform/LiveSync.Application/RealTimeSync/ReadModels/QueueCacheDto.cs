using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.ReadModels;

public sealed class QueueCacheDto : ICacheDto
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    public string FrontEndId => new FrontEndId(TopicBucket.Queue, Id).Value;
    public TopicBucket Bucket => TopicBucket.Queue;
}

using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.ReadModels;

public sealed class ItemCacheDto : ICacheDto
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public int ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    public string FrontEndId => new FrontEndId(TopicBucket.Item, Id).Value;
    public TopicBucket Bucket => TopicBucket.Item;
}

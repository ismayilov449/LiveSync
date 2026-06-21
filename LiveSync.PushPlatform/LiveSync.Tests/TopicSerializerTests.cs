using LiveSync.Domain.Enums;
using LiveSync.Domain.Serialization;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Tests;

public sealed class TopicSerializerTests
{
    [Fact]
    public void Serialize_Deserialize_RoundTrips_FilterWithSpecialCharacters()
    {
        var topic = new Topic(1, TopicBucket.Item, "item.Name.Contains(\":\")");

        var serialized = TopicSerializer.Serialize(topic);
        var restored = TopicSerializer.Deserialize(serialized);

        Assert.Equal(topic.Hash, restored.Hash);
        Assert.Equal(topic.Filter, restored.Filter);
    }

    [Fact]
    public void Deserialize_LegacyFormat_IsSupported()
    {
        var topic = TopicSerializer.Deserialize("tenantId#1:filter#item.ParentId == 5:bucket#Item");

        Assert.Equal(1, topic.TenantId);
        Assert.Equal(TopicBucket.Item, topic.Bucket);
        Assert.Equal("item.ParentId == 5", topic.Filter);
    }
}

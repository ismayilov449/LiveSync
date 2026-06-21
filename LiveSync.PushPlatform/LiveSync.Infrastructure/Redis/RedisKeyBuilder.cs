using LiveSync.Domain.Enums;

namespace LiveSync.Infrastructure.Redis;

public static class RedisKeyBuilder
{
    public static string Subscription(int tenantId, string subscriptionId)
        => $"{tenantId}:livesync:sub:{subscriptionId}";

    public static string ConnectionSubscriptions(int tenantId, string connectionId)
        => $"{tenantId}:livesync:subs:connection:{connectionId}";

    public static string TopicSubscriptions(int tenantId, string topicHash)
        => $"{tenantId}:livesync:subs:topic:{topicHash}";

    public static string BucketTopics(int tenantId, TopicBucket bucket)
        => $"{tenantId}:livesync:topics:bucket:{bucket}";

    public static string SubscriptionRenewalIndex()
        => "livesync:subs:renewal";
}
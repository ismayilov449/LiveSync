using LiveSync.Domain.Enums;

namespace LiveSync.Application.Hubs;

public static class PushHubGroups
{
    public static string TenantBucket(int tenantId, TopicBucket bucket)
        => $"tenant:{tenantId}:bucket:{bucket.ToString().ToLowerInvariant()}";
}

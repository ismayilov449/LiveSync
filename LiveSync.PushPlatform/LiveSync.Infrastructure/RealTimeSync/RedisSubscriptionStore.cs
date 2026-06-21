using System.Text.Json;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;
using LiveSync.Infrastructure.Redis;
using StackExchange.Redis;

namespace LiveSync.Infrastructure.RealTimeSync;

public sealed class RedisSubscriptionStore(IRedisConnectionFactory redis) : ISubscriptionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task AddAsync(SubscriptionDto subscription, CancellationToken ct = default)
    {
        var db = redis.Database;
        var json = JsonSerializer.Serialize(subscription, JsonOptions);

        await db.StringSetAsync(RedisKeyBuilder.Subscription(subscription.TenantId, subscription.Id), json);
        await db.SetAddAsync(RedisKeyBuilder.ConnectionSubscriptions(subscription.TenantId, subscription.ConnectionId), subscription.Id);
        await db.SetAddAsync(RedisKeyBuilder.TopicSubscriptions(subscription.TenantId, subscription.Topic.Hash), subscription.Id);
        await db.SetAddAsync(RedisKeyBuilder.BucketTopics(subscription.TenantId, subscription.Topic.Bucket), subscription.Topic.Key);

        await db.SortedSetAddAsync(
            RedisKeyBuilder.SubscriptionRenewalIndex(),
            RedisKeyBuilder.Subscription(subscription.TenantId, subscription.Id),
            subscription.RenewedAtUtc.ToUnixTimeSeconds());
    }

    public async Task RemoveAsync(string subscriptionId, int tenantId, CancellationToken ct = default)
    {
        var sub = await GetByIdAsync(subscriptionId, tenantId, ct);
        if (sub is null) return;

        var db = redis.Database;

        await db.KeyDeleteAsync(RedisKeyBuilder.Subscription(tenantId, subscriptionId));
        await db.SetRemoveAsync(RedisKeyBuilder.ConnectionSubscriptions(tenantId, sub.ConnectionId), subscriptionId);
        await db.SetRemoveAsync(RedisKeyBuilder.TopicSubscriptions(tenantId, sub.Topic.Hash), subscriptionId);
        await db.SortedSetRemoveAsync(RedisKeyBuilder.SubscriptionRenewalIndex(), RedisKeyBuilder.Subscription(tenantId, subscriptionId));
    }

    public async Task RemoveByConnectionAsync(string connectionId, int tenantId, CancellationToken ct = default)
    {
        var subs = await GetByConnectionIdAsync(connectionId, tenantId, ct);

        foreach (var sub in subs)
            await RemoveAsync(sub.Id, tenantId, ct);
    }

    public async Task<IReadOnlyList<SubscriptionDto>> GetByConnectionIdAsync(string connectionId, int tenantId, CancellationToken ct = default)
    {
        var subIds = await redis.Database.SetMembersAsync(RedisKeyBuilder.ConnectionSubscriptions(tenantId, connectionId));
        var result = new List<SubscriptionDto>();

        foreach (var subId in subIds)
        {
            var sub = await GetByIdAsync(subId.ToString(), tenantId, ct);
            if (sub is not null)
                result.Add(sub);
        }

        return result;
    }

    public async Task<SubscriptionDto?> GetByIdAsync(string subscriptionId, int tenantId, CancellationToken ct = default)
    {
        var value = await redis.Database.StringGetAsync(RedisKeyBuilder.Subscription(tenantId, subscriptionId));
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<SubscriptionDto>(value.ToString(), JsonOptions);
    }

    public async Task<IReadOnlyList<SubscriptionDto>> GetByTopicHashAsync(int tenantId, string topicHash, CancellationToken ct = default)
    {
        var subIds = await redis.Database.SetMembersAsync(RedisKeyBuilder.TopicSubscriptions(tenantId, topicHash));
        var result = new List<SubscriptionDto>();

        foreach (var subId in subIds)
        {
            var sub = await GetByIdAsync(subId.ToString(), tenantId, ct);
            if (sub is not null) result.Add(sub);
        }

        return result;
    }

    public async Task<IReadOnlyList<Topic>> GetTopicsByBucketAsync(int tenantId, TopicBucket bucket, CancellationToken ct = default)
    {
        var topicKeys = await redis.Database.SetMembersAsync(RedisKeyBuilder.BucketTopics(tenantId, bucket));
        var topics = new List<Topic>();

        foreach (var topicKey in topicKeys)
        {
            // topic key format: tenantId#1:filter#item.parentId == 5:bucket#Item
            var text = topicKey.ToString();
            var parts = text.Split(':');
            var map = parts
                .Select(p => p.Split('#', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);

            topics.Add(new Topic(
                int.Parse(map["tenantId"]),
                Enum.Parse<TopicBucket>(map["bucket"]),
                map["filter"]));
        }

        return topics.DistinctBy(t => t.Hash).ToList();
    }

    public async Task RenewAsync(string subscriptionId, int tenantId, CancellationToken ct = default)
    {
        var sub = await GetByIdAsync(subscriptionId, tenantId, ct);
        if (sub is null) return;

        sub.RenewedAtUtc = DateTimeOffset.UtcNow;

        await redis.Database.StringSetAsync(
            RedisKeyBuilder.Subscription(tenantId, subscriptionId),
            JsonSerializer.Serialize(sub, JsonOptions));

        await redis.Database.SortedSetAddAsync(
            RedisKeyBuilder.SubscriptionRenewalIndex(),
            RedisKeyBuilder.Subscription(tenantId, subscriptionId),
            sub.RenewedAtUtc.ToUnixTimeSeconds());
    }

    public async Task<IReadOnlyList<string>> GetExpiredSubscriptionKeysAsync(DateTimeOffset olderThan, CancellationToken ct = default)
    {
        var cutoff = olderThan.ToUnixTimeSeconds();
        var keys = await redis.Database.SortedSetRangeByScoreAsync(
            RedisKeyBuilder.SubscriptionRenewalIndex(),
            double.NegativeInfinity,
            cutoff);

        return keys.Select(k => k.ToString()).ToList();
    }

    public Task RemoveTopicFromBucketAsync(int tenantId, Topic topic, CancellationToken ct = default)
        => redis.Database.SetRemoveAsync(RedisKeyBuilder.BucketTopics(tenantId, topic.Bucket), topic.Key);
}
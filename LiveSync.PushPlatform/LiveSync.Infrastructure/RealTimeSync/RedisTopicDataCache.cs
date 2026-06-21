using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.ValueObjects;
using LiveSync.Infrastructure.Redis;

namespace LiveSync.Infrastructure.RealTimeSync;

public sealed class RedisTopicDataCache(
    IRedisConnectionFactory redis,
    BucketModuleRegistry registry) : ITopicDataCache
{
    public async Task<IDictionary<string, ICacheDto>> GetAllAsync(Topic topic, CancellationToken ct = default)
    {
        var module = registry.GetRequired(topic.Bucket);
        var entries = await redis.Database.HashGetAllAsync(topic.Key);
        var result = new Dictionary<string, ICacheDto>();

        foreach (var entry in entries)
        {
            var dto = module.Deserialize(entry.Value.ToString());
            if (dto is not null)
                result[entry.Name.ToString()] = dto;
        }

        return result;
    }

    public async Task SetAllAsync(Topic topic, IDictionary<string, ICacheDto> data, CancellationToken ct = default)
    {
        await redis.Database.KeyDeleteAsync(topic.Key);

        foreach (var kvp in data)
        {
            await redis.Database.HashSetAsync(
                topic.Key,
                kvp.Key,
                System.Text.Json.JsonSerializer.Serialize(kvp.Value, kvp.Value.GetType(), RedisJson.Options));
        }
    }

    public Task UpsertAsync(Topic topic, string frontEndId, ICacheDto data, CancellationToken ct = default)
    {
        return redis.Database.HashSetAsync(
            topic.Key,
            frontEndId,
            System.Text.Json.JsonSerializer.Serialize(data, data.GetType(), RedisJson.Options));
    }

    public async Task<bool> DeleteAsync(Topic topic, string frontEndId, CancellationToken ct = default)
        => await redis.Database.HashDeleteAsync(topic.Key, frontEndId);

    public Task DeleteTopicAsync(Topic topic, CancellationToken ct = default)
        => redis.Database.KeyDeleteAsync(topic.Key);
}

internal static class RedisJson
{
    internal static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };
}

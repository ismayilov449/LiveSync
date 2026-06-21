using System.Text.Json;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.ValueObjects;
using LiveSync.Infrastructure.Redis;

namespace LiveSync.Infrastructure.RealTimeSync;

public sealed class RedisTopicDataCache(IRedisConnectionFactory redis) : ITopicDataCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<IDictionary<string, ICacheDto>> GetAllAsync(Topic topic, CancellationToken ct = default)
    {
        var entries = await redis.Database.HashGetAllAsync(topic.Key);
        var result = new Dictionary<string, ICacheDto>();

        foreach (var entry in entries)
        {
            var dto = JsonSerializer.Deserialize<ItemCacheDto>(entry.Value.ToString(), JsonOptions);
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
                JsonSerializer.Serialize(kvp.Value, JsonOptions));
        }
    }

    public Task UpsertAsync(Topic topic, string froentEndId, ICacheDto data, CancellationToken ct = default)
    {
        return redis.Database.HashSetAsync(
            topic.Key,
            froentEndId,
            JsonSerializer.Serialize(data, JsonOptions));
    }

    public async Task<bool> DeleteAsync(Topic topic, string froentEndId, CancellationToken ct = default)
        => await redis.Database.HashDeleteAsync(topic.Key, froentEndId);

    public Task DeleteTopicAsync(Topic topic, CancellationToken ct = default)
        => redis.Database.KeyDeleteAsync(topic.Key);
}
using System.Text.Json;
using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Domain.Serialization;

public static class TopicSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(Topic topic)
        => JsonSerializer.Serialize(new TopicPayload(topic.TenantId, topic.Bucket, topic.Filter), JsonOptions);

    public static Topic Deserialize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Topic value is required.", nameof(value));

        if (value.TrimStart().StartsWith('{'))
            return DeserializeJson(value);

        return DeserializeLegacy(value);
    }

    private static Topic DeserializeJson(string json)
    {
        var payload = JsonSerializer.Deserialize<TopicPayload>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid topic JSON payload.");

        return new Topic(payload.TenantId, payload.Bucket, payload.Filter);
    }

    private static Topic DeserializeLegacy(string text)
    {
        var parts = text.Split(':');
        var map = parts
            .Select(p => p.Split('#', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

        return new Topic(
            int.Parse(map["tenantId"]),
            Enum.Parse<TopicBucket>(map["bucket"]),
            map["filter"]);
    }

    private sealed record TopicPayload(int TenantId, TopicBucket Bucket, string Filter);
}

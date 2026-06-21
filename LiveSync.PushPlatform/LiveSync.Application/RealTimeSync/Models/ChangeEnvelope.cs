using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.Models;

public sealed record ChangeEnvelope
{
    public required string Key { get; init; }
    public string? Payload { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }

    public TopicBucket Bucket { get; init; }
    public int TenantId { get; init; }
    public ChangeType EventType { get; init; }
    public int EntityId { get; init; }

    public static ChangeEnvelope Parse(string key, string? payload, DateTimeOffset? createdAt)
    {
        var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var map = parts
            .Select(p => p.Split('#', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

        var tenantId = int.Parse(map.TryGetValue("tenantId", out var t1) ? t1 : map["tenant"]);
        var eventType = Enum.Parse<ChangeType>(
            map.TryGetValue("eventType", out var e1) ? e1 : map["event"]);

        return new ChangeEnvelope
        {
            Key = key,
            Payload = payload,
            CreatedAt = createdAt,
            Bucket = Enum.Parse<TopicBucket>(map["table"]),
            TenantId = tenantId,
            EventType = eventType,
            EntityId = ExtractEntityId(payload)
        };
    }

    private static int ExtractEntityId(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return 0;

        using var doc = System.Text.Json.JsonDocument.Parse(payload);
        if (doc.RootElement.TryGetProperty("id", out var id)) return id.GetInt32();

        return 0;
    }

}

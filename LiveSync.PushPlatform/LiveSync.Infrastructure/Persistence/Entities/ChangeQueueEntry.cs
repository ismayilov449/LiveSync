namespace LiveSync.Infrastructure.Persistence.Entities;

public sealed class ChangeQueueEntry
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string Version { get; set; } = "1";
    public DateTimeOffset CreatedAt { get; set; }

    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public string? ClaimToken { get; set; }
}

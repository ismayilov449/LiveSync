namespace LiveSync.Application.Configuration;

public sealed class ChangeDetectionSettings
{
    public const string SectionName = "ChangeDetection";
    public bool Enabled { get; set; } = true;
    public int PollIntervalMs { get; set; } = 1000;
    public string QueueVersion { get; set; } = "1";
    public int TenantId { get; set; } = 1;
    public string DistributedLockName { get; set; } = "livesync-change-detection";
    public int MaxRetries { get; set; } = 5;
    public int BatchSize { get; set; } = 50;
    public int SubscriptionTtlSeconds { get; set; } = 300; // 5 min without renew
    public int ExpiryScanIntervalMs { get; set; } = 60000;
}

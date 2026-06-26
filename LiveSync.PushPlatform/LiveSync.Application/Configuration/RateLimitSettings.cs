namespace LiveSync.Application.Configuration;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public int TenantPermitLimit { get; set; } = 200;
    public int TenantWindowSeconds { get; set; } = 60;
}

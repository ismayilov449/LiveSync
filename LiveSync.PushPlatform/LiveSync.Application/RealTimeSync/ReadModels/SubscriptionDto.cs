using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.ReadModels;

public sealed class SubscriptionDto
{
    public required string Id { get; init; }
    public required string ConnectionId { get; init; }
    public required int TenantId { get; init; }
    public required int UserId { get; init; }
    public required Topic Topic { get; init; }
    public DateTimeOffset RenewedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

namespace LiveSync.Application.RealTimeSync.Contracts;

public sealed class FindAndSubscribeResponse
{
    public required string SubscriptionId { get; init; }
    public required IDictionary<string, object?> Data { get; init; }
}

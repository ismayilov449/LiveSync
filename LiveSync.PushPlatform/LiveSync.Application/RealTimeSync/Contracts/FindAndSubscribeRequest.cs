namespace LiveSync.Application.RealTimeSync.Contracts;

public sealed class FindAndSubscribeRequest
{
    public required string Bucket { get; init; }
    public required string Filter { get; init; }
}

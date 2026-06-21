namespace LiveSync.Application.RealTimeSync.Models;

public sealed record QueuedChange
{
    public required long QueueEntryId { get; init; }
    public required string ClaimToken { get; init; }
    public required ChangeEnvelope Envelope { get; init; }
}
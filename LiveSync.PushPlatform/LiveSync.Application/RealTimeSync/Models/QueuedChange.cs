namespace LiveSync.Application.RealTimeSync.Models;

public sealed class QueuedChange
{
    public required long QueueEntryId { get; init; }
    public required ChangeEnvelope Envelope { get; init; }
}
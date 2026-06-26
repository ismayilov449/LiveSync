using LiveSync.Domain.Enums;
using LiveSync.Domain.ValueObjects;

namespace LiveSync.Application.RealTimeSync.ReadModels;

public sealed class TicketCacheDto : ICacheDto
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public int QueueId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public TicketStatus Status { get; init; }
    public TicketPriority Priority { get; init; }
    public int? AssigneeUserId { get; init; }
    public bool IsActive { get; init; }

    public string FrontEndId => new FrontEndId(TopicBucket.Ticket, Id).Value;
    public TopicBucket Bucket => TopicBucket.Ticket;
}

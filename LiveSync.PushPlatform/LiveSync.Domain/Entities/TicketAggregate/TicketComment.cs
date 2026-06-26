using LiveSync.Domain.Enums;

namespace LiveSync.Domain.Entities.TicketAggregate;

public sealed class TicketComment
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public int AuthorUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private TicketComment() { }

    internal static TicketComment Create(int ticketId, int authorUserId, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Comment body is required.");

        return new TicketComment
        {
            TicketId = ticketId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}

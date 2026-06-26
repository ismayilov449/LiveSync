using LiveSync.Domain.Common;
using LiveSync.Domain.Entities.TicketAggregate.Events;
using LiveSync.Domain.Enums;
using LiveSync.Domain.Interfaces;

namespace LiveSync.Domain.Entities.TicketAggregate;

public sealed class Ticket : AggregateRoot, IAggregateRoot
{
    private readonly List<TicketComment> _comments = [];

    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public int QueueId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public int ReporterUserId { get; private set; }
    public int? AssigneeUserId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<TicketComment> Comments => _comments;

    private Ticket() { }

    private Ticket(
        int tenantId,
        int queueId,
        string subject,
        string description,
        TicketPriority priority,
        int reporterUserId)
    {
        TenantId = tenantId;
        QueueId = queueId;
        Subject = subject;
        Description = description;
        Priority = priority;
        ReporterUserId = reporterUserId;
        Status = TicketStatus.New;
        IsActive = true;
        var now = DateTime.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static Ticket Open(
        int tenantId,
        int queueId,
        string subject,
        string description,
        TicketPriority priority,
        int reporterUserId)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.");

        return new Ticket(tenantId, queueId, subject.Trim(), description?.Trim() ?? string.Empty, priority, reporterUserId);
    }

    public void CompleteCreation()
    {
        if (Id <= 0)
            throw new InvalidOperationException("Ticket id must be assigned before completing creation.");

        Raise(new TicketOpenedDomainEvent(TenantId, Id));
    }

    public void Assign(int assigneeUserId)
    {
        EnsureNotClosed();
        if (assigneeUserId <= 0)
            throw new ArgumentException("Assignee user id is required.");

        AssigneeUserId = assigneeUserId;
        if (Status == TicketStatus.New)
            Status = TicketStatus.Assigned;

        Touch();
        Raise(new TicketAssignedDomainEvent(TenantId, Id, assigneeUserId));
    }

    public void AddComment(int authorUserId, string body)
    {
        EnsureNotClosed();
        if (authorUserId <= 0)
            throw new ArgumentException("Author user id is required.");

        var comment = TicketComment.Create(Id, authorUserId, body);
        _comments.Add(comment);
        Touch();
        Raise(new TicketCommentAddedDomainEvent(TenantId, Id));
    }

    public void StartProgress()
    {
        EnsureNotClosed();
        if (Status is not (TicketStatus.New or TicketStatus.Assigned))
            throw new InvalidOperationException($"Cannot start progress from status {Status}.");

        if (!AssigneeUserId.HasValue)
            throw new InvalidOperationException("Ticket must be assigned before starting progress.");

        Status = TicketStatus.InProgress;
        Touch();
        Raise(new TicketStatusChangedDomainEvent(TenantId, Id, Status));
    }

    public void Resolve()
    {
        EnsureNotClosed();
        if (Status != TicketStatus.InProgress)
            throw new InvalidOperationException("Only in-progress tickets can be resolved.");

        Status = TicketStatus.Resolved;
        Touch();
        Raise(new TicketStatusChangedDomainEvent(TenantId, Id, Status));
    }

    public void Close()
    {
        if (Status == TicketStatus.Closed)
            return;

        if (Status != TicketStatus.Resolved)
            throw new InvalidOperationException("Only resolved tickets can be closed.");

        Status = TicketStatus.Closed;
        Touch();
        Raise(new TicketStatusChangedDomainEvent(TenantId, Id, Status));
    }

    public void MarkDeleted()
        => Raise(new TicketDeletedDomainEvent(TenantId, Id));

    private void EnsureNotClosed()
    {
        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException("Ticket is closed.");
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

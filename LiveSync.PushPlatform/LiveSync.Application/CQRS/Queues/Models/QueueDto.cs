namespace LiveSync.Application.CQRS.Queues.Models;

public sealed record QueueDto(
    int Id,
    int TenantId,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc);

namespace LiveSync.Application.CQRS.Queues.Models;

public sealed record PagedQueuesResponse(
    IReadOnlyList<QueueDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

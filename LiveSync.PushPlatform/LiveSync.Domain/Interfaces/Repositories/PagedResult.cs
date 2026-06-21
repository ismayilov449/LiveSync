namespace LiveSync.Domain.Interfaces.Repositories;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);

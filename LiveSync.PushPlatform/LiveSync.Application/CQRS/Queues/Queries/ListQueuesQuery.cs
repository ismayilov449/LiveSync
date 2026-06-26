using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Queues.Models;

namespace LiveSync.Application.CQRS.Queues.Queries;

public sealed record ListQueuesQuery(int TenantId, int Page = 1, int PageSize = 20)
    : IQuery<PagedQueuesResponse>;

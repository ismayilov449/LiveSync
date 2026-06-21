using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Models;

namespace LiveSync.Application.CQRS.Items.Queries;

public sealed record GetItemByIdQuery(int TenantId, int Id) : IQuery<ItemDto?>;

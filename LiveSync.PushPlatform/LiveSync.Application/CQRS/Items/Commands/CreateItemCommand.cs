using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed record CreateItemCommand(int TenantId, int ParentId, string Name) : ICommand<int>;

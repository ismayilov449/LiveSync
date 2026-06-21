using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.Items.Commands.CreateItem;

public sealed record CreateItemCommand(int TenantId, int ParentId, string Name) : ICommand<int>;

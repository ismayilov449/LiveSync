using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.Items.Commands.MoveItem;

public sealed record MoveItemCommand(int TenantId, int Id, int ParentId) : ICommand;

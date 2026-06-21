using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed record MoveItemCommand(int TenantId, int Id, int ParentId) : ICommand;

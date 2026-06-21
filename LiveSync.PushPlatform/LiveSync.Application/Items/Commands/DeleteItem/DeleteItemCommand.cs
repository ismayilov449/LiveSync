using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.Items.Commands.DeleteItem;

public sealed record DeleteItemCommand(int TenantId, int Id) : ICommand;

using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.Items.Commands.UpdateItem;

public sealed record UpdateItemCommand(int TenantId, int Id, string Name) : ICommand;

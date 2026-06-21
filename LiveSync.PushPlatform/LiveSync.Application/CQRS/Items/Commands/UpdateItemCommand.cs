using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed record UpdateItemCommand(int TenantId, int Id, string Name) : ICommand;

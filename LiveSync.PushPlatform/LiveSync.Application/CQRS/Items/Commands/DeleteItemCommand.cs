using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed record DeleteItemCommand(int TenantId, int Id) : ICommand;

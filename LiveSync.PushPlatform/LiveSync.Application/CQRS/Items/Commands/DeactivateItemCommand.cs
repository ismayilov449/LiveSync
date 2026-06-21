using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Items.Commands;

public sealed record DeactivateItemCommand(int TenantId, int Id) : ICommand;

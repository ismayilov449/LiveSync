using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.Items.Commands.DeactivateItem;

public sealed record DeactivateItemCommand(int TenantId, int Id) : ICommand;

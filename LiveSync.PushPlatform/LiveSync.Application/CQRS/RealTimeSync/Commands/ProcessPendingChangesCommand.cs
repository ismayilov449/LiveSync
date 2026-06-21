using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.RealTimeSync.Commands;

public sealed record ProcessPendingChangesCommand : ICommand;

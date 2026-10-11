namespace LiveSync.Application.Common.Interfaces;

/// <summary>
/// Marks a command whose aggregate changes and change queue (outbox) rows must commit in one transaction.
/// Handled by <see cref="Behaviors.TransactionBehavior{TRequest, TResponse}"/>.
/// </summary>
public interface ITransactionalCommand;

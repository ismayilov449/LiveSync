using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.RealTimeSync.Services;
using MediatR;

namespace LiveSync.Application.Common.Behaviors;

/// <summary>
/// Transactional outbox boundary: the handler's aggregate changes and the change queue rows written by
/// domain event handlers commit together. Immediate pushes are sent only after the commit succeeds.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    ITransactionRunner transactionRunner,
    PostCommitNotifications notifications)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalCommand
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        TResponse response;
        try
        {
            response = await transactionRunner.ExecuteAsync(_ =>
            {
                // A retried attempt must not push changes raised by the rolled-back one.
                notifications.Clear();
                return next();
            }, ct);
        }
        catch
        {
            notifications.Clear();
            throw;
        }

        await notifications.FlushAsync(ct);
        return response;
    }
}

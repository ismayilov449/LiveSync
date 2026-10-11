namespace LiveSync.Application.Common.Interfaces;

public interface ITransactionRunner
{
    /// <summary>
    /// Runs <paramref name="operation"/> inside a tenant database transaction and commits it.
    /// On a transient SQL failure the whole operation is rolled back and run again.
    /// </summary>
    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken ct = default);
}

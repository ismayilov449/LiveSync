namespace LiveSync.Application.RealTimeSync.Ports;

public interface IDistributedLock : IAsyncDisposable
{
    bool IsAcquired { get; }
    Task<bool> RenewAsync(TimeSpan expiry, CancellationToken ct = default);
}

public interface IDistributedLockFactory
{
    Task<IDistributedLock> AcquireAsync(string resource, TimeSpan expiry, CancellationToken ct = default);
}

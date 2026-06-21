namespace LiveSync.Application.RealTimeSync.Ports;

public interface IDistributedLock : IAsyncDisposable
{
    bool IsAcquired { get; }
}

public interface IDistributedLockFactory
{
    Task<IDistributedLock> AcquireAsync(string resource, TimeSpan expiry, CancellationToken ct = default);
}

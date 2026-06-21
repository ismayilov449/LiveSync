using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Infrastructure.Redis;
using StackExchange.Redis;

namespace LiveSync.Infrastructure.Locking;

public sealed class RedisDistributedLockFactory(IRedisConnectionFactory redis) : IDistributedLockFactory
{
    public async Task<IDistributedLock> AcquireAsync(string resource, TimeSpan expiry, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        var key = $"lock:{resource}";
        var acquired = await redis.Database.StringSetAsync(key, token, expiry, When.NotExists);

        return new RedisDistributedLock(redis.Database, key, token, acquired);
    }
}

internal sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _db;
    private readonly string _key;
    private readonly string _token;

    public bool IsAcquired { get; }

    public RedisDistributedLock(IDatabase db, string key, string token, bool isAcquired)
    {
        _db = db;
        _key = key;
        _token = token;
        IsAcquired = isAcquired;
    }

    public async Task<bool> RenewAsync(TimeSpan expiry, CancellationToken ct = default)
    {
        if (!IsAcquired)
            return false;

        const string script = """
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('pexpire', KEYS[1], ARGV[2])
            else
                return 0
            end
            """;

        var result = await _db.ScriptEvaluateAsync(
            script,
            [_key],
            [_token, ((long)expiry.TotalMilliseconds).ToString()]);

        return (int)result == 1;
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsAcquired) return;

        const string script = """
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end
            """;

        await _db.ScriptEvaluateAsync(script, [_key], [_token]);
    }
}

using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace LiveSync.Infrastructure.Redis;

public interface IRedisConnectionFactory
{
    IConnectionMultiplexer Connection { get; }
    IDatabase Database { get; }
}

public sealed class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    public RedisConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        Connection = ConnectionMultiplexer.Connect(connectionString);
        Database = Connection.GetDatabase();
    }

    public IConnectionMultiplexer Connection { get; }
    public IDatabase Database { get; }

    public void Dispose() => Connection.Dispose();
}
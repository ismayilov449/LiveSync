using LiveSync.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using StackExchange.Redis;

namespace LiveSync.Infrastructure.Redis;

public interface IRedisConnectionFactory
{
    IConnectionMultiplexer Connection { get; }
    IDatabase Database { get; }
}

public sealed class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private static readonly ResiliencePipeline ConnectPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<RedisConnectionException>()
        })
        .Build();

    public RedisConnectionFactory(IConfiguration configuration, ILogger<RedisConnectionFactory> logger)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        Connection = ConnectPipeline.Execute(() =>
        {
            logger.LogInformation("Connecting to Redis...");
            return ConnectionMultiplexer.Connect(connectionString);
        });

        Database = Connection.GetDatabase();
    }

    public IConnectionMultiplexer Connection { get; }
    public IDatabase Database { get; }

    public void Dispose() => Connection.Dispose();
}

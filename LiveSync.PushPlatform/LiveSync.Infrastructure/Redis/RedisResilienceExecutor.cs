using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using StackExchange.Redis;

namespace LiveSync.Infrastructure.Redis;

public sealed class RedisResilienceExecutor
{
    private readonly ResiliencePipeline _pipeline;

    public RedisResilienceExecutor()
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<RedisException>().Handle<RedisTimeoutException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder().Handle<RedisException>().Handle<RedisTimeoutException>()
            })
            .Build();
    }

    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
        => _pipeline.ExecuteAsync<T>(
            token => new ValueTask<T>(action(token)),
            ct).AsTask();

    public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        => _pipeline.ExecuteAsync(
            token => new ValueTask(action(token)),
            ct).AsTask();
}

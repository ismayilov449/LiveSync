using LiveSync.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LiveSync.Infrastructure.Extensions;

public static class LiveSyncSignalR
{
    public const string RedisChannelPrefix = "LiveSync";

    public static IServiceCollection AddLiveSyncSignalR(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        var redisOptions = ConfigurationOptions.Parse(redis);
        redisOptions.ChannelPrefix = RedisChannel.Literal(RedisChannelPrefix);

        services.AddSignalR()
            .AddStackExchangeRedis(o =>
            {
                o.Configuration = redisOptions;
            });

        return services;
    }
}

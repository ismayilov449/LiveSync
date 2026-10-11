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
        services.AddSignalR()
            .AddStackExchangeRedis(o =>
            {
                // Read when options are built, not at registration, so configuration added later
                // by the host (e.g. integration test containers) is honored.
                var redis = configuration.GetConnectionString("Redis")
                    ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

                o.Configuration = ConfigurationOptions.Parse(redis);
                o.Configuration.ChannelPrefix = RedisChannel.Literal(RedisChannelPrefix);
            });

        return services;
    }
}

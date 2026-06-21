using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LiveSync.Infrastructure.Extensions;

public static class LiveSyncSignalR
{
    public static IServiceCollection AddLiveSyncSignalR(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddSignalR()
            .AddStackExchangeRedis(o =>
            {
                o.Configuration = ConfigurationOptions.Parse(redis);
            });

        return services;
    }
}

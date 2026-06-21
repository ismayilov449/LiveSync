using LiveSync.Application.Configuration;
using LiveSync.Infrastructure.HostedServices;
using Microsoft.Extensions.Hosting;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Common;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using LiveSync.Infrastructure.DomainEvents;
using LiveSync.Infrastructure.Integration.ChangeQueue;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.Repositories;
using LiveSync.Infrastructure.RealTimeSync;
using LiveSync.Infrastructure.Redis;
using LiveSync.Infrastructure.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LiveSync.Infrastructure.Locking;

namespace LiveSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // SQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // Change queue + DTO loading
        services.AddScoped<IChangeQueueStore, SqlChangeQueueStore>();
        services.AddScoped<ICacheDtoProvider, CacheDtoProvider>();

        // Redis
        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.AddScoped<ISubscriptionStore, RedisSubscriptionStore>();
        services.AddScoped<ITopicDataCache, RedisTopicDataCache>();
        services.AddSingleton<IDistributedLockFactory, RedisDistributedLockFactory>();

        // Push pipeline
        services.AddScoped<IFilterEvaluator, DynamicExpressoFilterEvaluator>();
        services.AddScoped<IRealTimeNotifier, SignalRRealTimeNotifier>();

        services.Configure<ChangeDetectionSettings>(
            configuration.GetSection(ChangeDetectionSettings.SectionName));

        services.AddHostedService<ChangeDetectionHostedService>();
        services.AddHostedService<SubscriptionExpiryHostedService>();

        return services;
    }
}
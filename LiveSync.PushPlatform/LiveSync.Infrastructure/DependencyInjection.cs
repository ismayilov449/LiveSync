using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using LiveSync.Infrastructure.Configuration;
using LiveSync.Infrastructure.HostedServices;
using LiveSync.Infrastructure.Locking;
using Microsoft.Extensions.Hosting;
using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.Handlers;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Common;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;
using LiveSync.Infrastructure.DomainEvents;
using LiveSync.Infrastructure.Integration.ChangeQueue;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.Repositories;
using LiveSync.Infrastructure.RealTimeSync;
using LiveSync.Infrastructure.RealTimeSync.Buckets;
using LiveSync.Infrastructure.Redis;
using LiveSync.Infrastructure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LiveSync.Infrastructure.Audit;
using LiveSync.Infrastructure.Persistence.Idempotency;
using LiveSync.Infrastructure.Tenancy;

namespace LiveSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<InfrastructureHostingOptions>? configureHosting = null)
    {
        var hostingOptions = new InfrastructureHostingOptions();
        configureHosting?.Invoke(hostingOptions);

        services.AddLiveSyncTenancy(configuration);

        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        services.AddScoped<IChangeQueueStore, SqlChangeQueueStore>();
        services.AddScoped<ICacheDtoProvider, CacheDtoProvider>();
        services.AddScoped<IIdempotencyStore, SqlIdempotencyStore>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITenantLifecycleService, TenantLifecycleService>();

        services.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
        services.AddSingleton<RedisResilienceExecutor>();
        services.AddScoped<ISubscriptionStore, RedisSubscriptionStore>();
        services.AddScoped<ITopicDataCache, RedisTopicDataCache>();
        services.AddSingleton<IDistributedLockFactory, RedisDistributedLockFactory>();

        services.AddScoped<IBucketModule, ItemBucketModule>();
        services.AddScoped<BucketModuleRegistry>();

        services.AddScoped<IFilterEvaluator, DynamicExpressoFilterEvaluator>();
        services.AddScoped<IRealTimeNotifier, SignalRRealTimeNotifier>();

        services.Configure<ChangeDetectionSettings>(
            configuration.GetSection(ChangeDetectionSettings.SectionName));

        if (hostingOptions.RunChangeDetection)
            services.AddHostedService<ChangeDetectionHostedService>();

        services.AddHostedService<ChangeQueueMetricsHostedService>();

        if (hostingOptions.RunSubscriptionExpiry)
            services.AddHostedService<SubscriptionExpiryHostedService>();

        services.AddSingleton(hostingOptions);

        return services;
    }
}

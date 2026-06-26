using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using LiveSync.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSync.Infrastructure;

public static class TenancyDependencyInjection
{
    public static IServiceCollection AddLiveSyncTenancy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TenancySettings>(configuration.GetSection(TenancySettings.SectionName));
        services.AddMemoryCache();

        services.AddDbContext<MasterDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ControlPlane"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddScoped<ITenantRegistry, TenantRegistry>();
        services.AddScoped<ITenantProvisioner, TenantProvisioner>();
        services.AddScoped<ITenantItemBootstrap, TenantItemBootstrap>();
        services.AddScoped<ITenantAccessValidator, TenantAccessValidator>();
        services.AddScoped<ISharedDatabaseMigrator, SharedDatabaseMigrator>();

        services.AddScoped<AppDbContext>(sp =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsSet)
            {
                var userContext = sp.GetService<IUserContext>();
                if (userContext is { IsAuthenticated: true })
                    tenantContext.SetTenantId(userContext.TenantId);
            }

            if (!tenantContext.IsSet)
                throw new InvalidOperationException(
                    "Tenant database access requires an authenticated tenant context.");

            var factory = sp.GetRequiredService<ITenantDbContextFactory>();
            return factory.CreateDbContext(tenantContext.TenantId, tenantContext);
        });

        return services;
    }
}

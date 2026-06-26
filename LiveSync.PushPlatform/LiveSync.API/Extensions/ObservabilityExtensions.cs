using LiveSync.Infrastructure.Observability;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

namespace LiveSync.API.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddLiveSyncObservability(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .WriteTo.Console());

        builder.Services.AddLiveSyncOpenTelemetry(builder.Configuration, builder.Environment);

        return builder;
    }

    public static IServiceCollection AddLiveSyncHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddHealthChecks()
            .AddDbContextCheck<MasterDbContext>("control-plane", tags: ["ready"])
            .AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapLiveSyncHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, _) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { status = "Alive" });
            }
        });

        return endpoints;
    }

    public static WebApplication MapLiveSyncObservabilityEndpoints(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint("/metrics");
        return app;
    }
}

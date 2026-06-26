using LiveSync.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LiveSync.Infrastructure.Observability;

public static class LiveSyncOpenTelemetryExtensions
{
    public static IServiceCollection AddLiveSyncOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var observability = configuration.GetSection(ObservabilitySettings.SectionName).Get<ObservabilitySettings>()
            ?? new ObservabilitySettings();

        services.Configure<ObservabilitySettings>(configuration.GetSection(ObservabilitySettings.SectionName));

        var otlpEndpoint = observability.Otlp.Endpoint;
        var hasOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(environment.ApplicationName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("LiveSync");

                if (hasOtlp)
                {
                    tracing.AddOtlpExporter(options =>
                        options.Endpoint = new Uri(otlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("LiveSync");

                if (observability.EnablePrometheus)
                    metrics.AddPrometheusExporter();

                if (hasOtlp)
                {
                    metrics.AddOtlpExporter(options =>
                        options.Endpoint = new Uri(otlpEndpoint!));
                }
            });

        return services;
    }
}

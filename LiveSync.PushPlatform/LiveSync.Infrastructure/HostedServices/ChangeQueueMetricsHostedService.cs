using LiveSync.Application.Configuration;
using LiveSync.Application.Observability;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSync.Infrastructure.HostedServices;

public sealed class ChangeQueueMetricsHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChangeDetectionSettings> settings,
    ILogger<ChangeQueueMetricsHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SampleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to sample change queue metrics.");
            }
        }
    }

    private async Task SampleAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var tenantIds = await tenantRegistry.GetActiveTenantIdsAsync(ct);
        if (tenantIds.Count == 0)
            return;

        var totalPending = 0;
        var totalDeadLetter = 0;

        foreach (var tenantId in tenantIds)
        {
            using var tenantScope = scopeFactory.CreateScope();
            tenantScope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenantId(tenantId);
            var queue = tenantScope.ServiceProvider.GetRequiredService<IChangeQueueStore>();
            var stats = await queue.GetStatisticsAsync(settings.Value.QueueVersion, ct);
            totalPending += stats.PendingCount;
            totalDeadLetter += stats.DeadLetterCount;
        }

        LiveSyncMetrics.SetQueueDepth(totalPending, totalDeadLetter);
    }
}

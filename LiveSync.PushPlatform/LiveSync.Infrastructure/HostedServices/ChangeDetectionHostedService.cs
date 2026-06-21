using LiveSync.Application.Configuration;
using LiveSync.Application.CQRS.RealTimeSync.Commands;
using LiveSync.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSync.Infrastructure.HostedServices;

public sealed class ChangeDetectionHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChangeDetectionSettings> settings,
    ILogger<ChangeDetectionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Value.Enabled)
        {
            logger.LogInformation("Change detection is disabled.");
            return;
        }

        logger.LogInformation(
            "Change detection started. PollInterval={PollIntervalMs}ms, QueueVersion={QueueVersion}",
            settings.Value.PollIntervalMs,
            settings.Value.QueueVersion);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(settings.Value.PollIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Change detection tick failed.");
            }
        }

        logger.LogInformation("Change detection stopped.");
    }

    private async Task ProcessTickAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
        var tenantIds = await tenantRegistry.GetActiveTenantIdsAsync(ct);
        if (tenantIds.Count == 0)
            return;

        foreach (var tenantId in tenantIds)
        {
            using var tenantScope = scopeFactory.CreateScope();
            tenantScope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenantId(tenantId);
            var mediator = tenantScope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(new ProcessPendingChangesCommand(), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Change detection failed for tenant {TenantId}", tenantId);
            }
        }
    }
}
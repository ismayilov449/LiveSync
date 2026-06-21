using LiveSync.Application.Configuration;
using LiveSync.Application.RealTimeSync.Changes;
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
        // Scoped services (DbContext, ChangeProcessor, etc.) need a scope per tick
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new ProcessPendingChangesCommand(), ct);
    }
}
using LiveSync.Application.Configuration;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSync.Infrastructure.HostedServices;

public sealed class SubscriptionExpiryHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChangeDetectionSettings> settings,
    ILogger<SubscriptionExpiryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Subscription expiry started. Ttl={TtlSeconds}s, ScanInterval={ScanIntervalMs}ms",
            settings.Value.SubscriptionTtlSeconds,
            settings.Value.ExpiryScanIntervalMs);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(settings.Value.ExpiryScanIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ScanExpiredSubscriptionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Subscription expiry scan failed.");
            }
        }
    }

    private async Task ScanExpiredSubscriptionsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var subscriptionStore = scope.ServiceProvider.GetRequiredService<ISubscriptionStore>();
        var subscriptionManager = scope.ServiceProvider.GetRequiredService<SubscriptionManager>();

        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-settings.Value.SubscriptionTtlSeconds);
        var expiredKeys = await subscriptionStore.GetExpiredSubscriptionKeysAsync(cutoff, ct);

        foreach (var key in expiredKeys)
        {
            var parts = key.Split(':');
            if (parts.Length < 4)
                continue;

            if (!int.TryParse(parts[0], out var tenantId))
                continue;

            var subscriptionId = parts[^1];
            await subscriptionManager.UnsubscribeAsync(subscriptionId, tenantId, ct);
            logger.LogInformation("Expired subscription removed. TenantId={TenantId}, SubscriptionId={SubscriptionId}", tenantId, subscriptionId);
        }
    }
}

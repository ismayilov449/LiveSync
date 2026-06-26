using LiveSync.Application.Hubs;
using LiveSync.Application.Observability;
using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace LiveSync.Infrastructure.SignalR;

public sealed class SignalRRealTimeNotifier(IHubContext<PushHub, IPushClient> hub)
    : IRealTimeNotifier
{
    public async Task NotifyAsync(string connectionId, ChangeNotificationDto notification, CancellationToken ct = default)
    {
        await hub.Clients.Client(connectionId).PushUpdate(notification);
        LiveSyncMetrics.SignalRPushes.Add(1);
    }

    public async Task NotifyBucketAsync(
        int tenantId,
        TopicBucket bucket,
        ChangeNotificationDto notification,
        CancellationToken ct = default)
    {
        await hub.Clients.Group(PushHubGroups.TenantBucket(tenantId, bucket)).PushUpdate(notification);
        LiveSyncMetrics.SignalRPushes.Add(1);
    }
}

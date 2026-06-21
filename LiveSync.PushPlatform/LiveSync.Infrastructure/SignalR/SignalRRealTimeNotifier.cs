using LiveSync.Application.Hubs;
using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Ports;
using Microsoft.AspNetCore.SignalR;

namespace LiveSync.Infrastructure.SignalR;

public sealed class SignalRRealTimeNotifier(IHubContext<PushHub, IPushClient> hub)
    : IRealTimeNotifier
{
    public Task NotifyAsync(string connectionId, ChangeNotificationDto notification, CancellationToken ct = default)
        => hub.Clients.Client(connectionId).PushUpdate(notification);
}
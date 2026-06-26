using LiveSync.Application.RealTimeSync.Contracts;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface IRealTimeNotifier
{
    Task NotifyAsync(string connectionId, ChangeNotificationDto notification, CancellationToken ct = default);

    Task NotifyTenantAsync(int tenantId, ChangeNotificationDto notification, CancellationToken ct = default);
}

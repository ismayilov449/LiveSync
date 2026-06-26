using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface IRealTimeNotifier
{
    Task NotifyAsync(string connectionId, ChangeNotificationDto notification, CancellationToken ct = default);

    Task NotifyBucketAsync(
        int tenantId,
        TopicBucket bucket,
        ChangeNotificationDto notification,
        CancellationToken ct = default);
}

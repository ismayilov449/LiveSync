using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LiveSync.Application.RealTimeSync.Services;

/// <summary>
/// Holds the API's immediate SignalR pushes until the command's transaction commits, so no client
/// is told about a change that was rolled back. Delivery is best-effort: the change queue row written
/// in the same transaction guarantees the worker pushes the change again.
/// </summary>
public sealed class PostCommitNotifications(
    IRealTimeNotifier notifier,
    ILogger<PostCommitNotifications> logger)
{
    private readonly List<PendingNotification> _pending = [];

    public void Add(int tenantId, TopicBucket bucket, ChangeNotificationDto notification)
        => _pending.Add(new PendingNotification(tenantId, bucket, notification));

    public void Clear() => _pending.Clear();

    public async Task FlushAsync(CancellationToken ct = default)
    {
        var pending = _pending.ToArray();
        _pending.Clear();

        foreach (var (tenantId, bucket, notification) in pending)
        {
            try
            {
                await notifier.NotifyBucketAsync(tenantId, bucket, notification, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Immediate push failed for tenant {TenantId} bucket {Bucket}; the worker will deliver it from the change queue.",
                    tenantId,
                    bucket);
            }
        }
    }

    private sealed record PendingNotification(int TenantId, TopicBucket Bucket, ChangeNotificationDto Notification);
}

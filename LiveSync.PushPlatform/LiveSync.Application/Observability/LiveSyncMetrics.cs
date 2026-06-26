using System.Diagnostics.Metrics;

namespace LiveSync.Application.Observability;

public static class LiveSyncMetrics
{
    public const string MeterName = "LiveSync";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static int _pendingDepth;
    private static int _deadLetterDepth;

    public static readonly Counter<long> ChangesProcessed =
        Meter.CreateCounter<long>("livesync.changes.processed", description: "Change queue entries processed successfully");

    public static readonly Counter<long> ChangesFailed =
        Meter.CreateCounter<long>("livesync.changes.failed", description: "Change queue processing failures (will retry)");

    public static readonly Counter<long> ChangesDeadLettered =
        Meter.CreateCounter<long>("livesync.changes.dead_lettered", description: "Change queue entries moved to dead-letter");

    public static readonly Counter<long> SignalRPushes =
        Meter.CreateCounter<long>("livesync.signalr.pushes", description: "SignalR push notifications sent");

    public static readonly Histogram<double> ChangeProcessingDuration =
        Meter.CreateHistogram<double>("livesync.changes.processing_duration_ms", unit: "ms",
            description: "Time to process a single change queue entry");

    static LiveSyncMetrics()
    {
        Meter.CreateObservableGauge(
            "livesync.change_queue.depth",
            () => _pendingDepth,
            description: "Pending change queue entries");

        Meter.CreateObservableGauge(
            "livesync.change_queue.dead_letter_depth",
            () => _deadLetterDepth,
            description: "Dead-letter change queue entries");
    }

    public static void SetQueueDepth(int pending, int deadLetter)
    {
        _pendingDepth = pending;
        _deadLetterDepth = deadLetter;
    }
}

using LiveSync.Application.RealTimeSync.Enums;

namespace LiveSync.Application.RealTimeSync.Contracts;

public sealed class ChangeNotificationDto
{
    public NotificationOperation Operation { get; init; }
    public required ChangeEntityDto Entity { get; set; }
    public object? Change { get; set; }
}

public sealed class ChangeEntityDto
{
    public required string Id { get; init; }
    public required string Bucket { get; init; }
}

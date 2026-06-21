using LiveSync.Application.RealTimeSync.Contracts;
namespace LiveSync.Application.Hubs;

public interface IPushClient
{
    Task PushUpdate(ChangeNotificationDto notification);
}
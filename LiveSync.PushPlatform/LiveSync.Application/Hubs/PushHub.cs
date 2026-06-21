using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Subscriptions;
using LiveSync.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LiveSync.Application.Hubs;

public sealed class PushHub(SubscriptionManager subscriptionManager, IUserContext userContext) : Hub<IPushClient>
{
    public async Task<FindAndSubscribeResponse> FindAndSubscribe(FindAndSubscribeRequest request)
    {
        return await subscriptionManager.FindAndSubscribeAsync(
            userContext.TenantId,
            userContext.UserId,
            Context.ConnectionId,
            request.Bucket,
            request.Filter);
    }

    public Task Unsubscribe(string subscriptionId)
        => subscriptionManager.UnsubscribeAsync(subscriptionId, userContext.TenantId);

    public Task Renew(string subscriptionId)
        => subscriptionManager.RenewAsync(subscriptionId, userContext.TenantId);

    public override Task OnDisconnectedAsync(Exception? exception)
        => subscriptionManager.RemoveConnectionAsync(Context.ConnectionId, userContext.TenantId);
}

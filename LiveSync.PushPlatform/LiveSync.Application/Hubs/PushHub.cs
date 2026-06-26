using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using LiveSync.Application.RealTimeSync.Contracts;
using LiveSync.Application.RealTimeSync.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace LiveSync.Application.Hubs;

[Authorize]
public sealed class PushHub(
    SubscriptionManager subscriptionManager,
    IUserContext userContext,
    IOptions<AuthSettings> authOptions) : Hub<IPushClient>
{
    public async Task<FindAndSubscribeResponse> FindAndSubscribe(FindAndSubscribeRequest request)
    {
        var tenantId = userContext.TenantId;

        await Groups.AddToGroupAsync(Context.ConnectionId, PushHubGroups.Tenant(tenantId));

        var response = await subscriptionManager.FindAndSubscribeAsync(
            tenantId,
            userContext.UserId,
            Context.ConnectionId,
            request.Bucket,
            request.Filter);

        return response;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = ResolveTenantId();
        if (tenantId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, PushHubGroups.Tenant(tenantId));

        await base.OnConnectedAsync();
    }

    public Task Unsubscribe(string subscriptionId)
        => subscriptionManager.UnsubscribeAsync(subscriptionId, userContext.TenantId);

    public Task Renew(string subscriptionId)
        => subscriptionManager.RenewAsync(subscriptionId, userContext.TenantId);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = ResolveTenantId();
        if (tenantId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, PushHubGroups.Tenant(tenantId));
            await subscriptionManager.RemoveConnectionAsync(Context.ConnectionId, tenantId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private int ResolveTenantId()
    {
        try
        {
            return userContext.TenantId;
        }
        catch (UnauthorizedAccessException)
        {
            var claimType = authOptions.Value.Claims.TenantId;
            var claim = Context.User.FindFirst(claimType);
            return claim is not null && int.TryParse(claim.Value, out var tenantId) ? tenantId : 0;
        }
    }
}

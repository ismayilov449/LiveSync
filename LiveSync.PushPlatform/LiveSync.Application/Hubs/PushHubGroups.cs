namespace LiveSync.Application.Hubs;

public static class PushHubGroups
{
    public static string Tenant(int tenantId) => $"tenant:{tenantId}";
}

namespace LiveSync.Application.Common.Interfaces;

public interface ITenantAccessValidator
{
    Task EnsureUserBelongsToTenantAsync(int userId, int tenantId, CancellationToken ct = default);
}

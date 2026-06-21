namespace LiveSync.Application.Common.Interfaces;

public interface ITenantContext
{
    int TenantId { get; }
    bool IsSet { get; }
    void SetTenantId(int tenantId);
}

using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    private int _tenantId;
    private bool _isSet;

    public int TenantId =>
        _isSet
            ? _tenantId
            : throw new InvalidOperationException("Tenant context has not been set for this scope.");

    public bool IsSet => _isSet;

    public void SetTenantId(int tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be positive.");

        if (_isSet && _tenantId != tenantId)
            throw new InvalidOperationException(
                $"Tenant context is already set to tenant {_tenantId} and cannot be changed to {tenantId}.");

        _tenantId = tenantId;
        _isSet = true;
    }
}

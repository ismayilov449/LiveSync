namespace LiveSync.Application.Common.Exceptions;

public sealed class TenantSuspendedException(int tenantId)
    : Exception($"Tenant {tenantId} is suspended.");

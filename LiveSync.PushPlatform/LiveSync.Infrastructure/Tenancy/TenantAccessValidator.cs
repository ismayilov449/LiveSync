using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Tenancy;

public sealed class TenantAccessValidator(MasterDbContext masterDb) : ITenantAccessValidator
{
    public async Task EnsureUserBelongsToTenantAsync(int userId, int tenantId, CancellationToken ct = default)
    {
        var belongs = await masterDb.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.TenantId == tenantId, ct);

        if (!belongs)
            throw new UnauthorizedAccessException(
                $"User {userId} is not authorized for tenant {tenantId}.");
    }
}

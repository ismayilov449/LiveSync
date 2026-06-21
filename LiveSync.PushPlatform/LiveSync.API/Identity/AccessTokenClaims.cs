using System.Security.Claims;
using LiveSync.Application.Configuration;

namespace LiveSync.API.Identity;

public sealed class AccessTokenClaims
{
    public required int TenantId { get; init; }
    public required int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;

    public static AccessTokenClaims? TryParse(IEnumerable<Claim> claims, ClaimSettings settings)
    {
        var lookup = claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);

        if (!TryReadPositiveInt(lookup, settings.TenantId, out var tenantId))
            return null;

        if (!TryReadPositiveInt(lookup, settings.UserId, out var userId))
            return null;

        lookup.TryGetValue(settings.UserName, out var userName);

        return new AccessTokenClaims
        {
            TenantId = tenantId,
            UserId = userId,
            UserName = userName ?? string.Empty
        };
    }

    private static bool TryReadPositiveInt(
        IReadOnlyDictionary<string, string> lookup,
        string claimType,
        out int value)
    {
        value = 0;
        return lookup.TryGetValue(claimType, out var raw)
            && int.TryParse(raw, out value)
            && value > 0;
    }
}

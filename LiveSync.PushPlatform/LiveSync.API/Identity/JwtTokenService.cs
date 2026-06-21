using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LiveSync.Application.Configuration;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LiveSync.API.Identity;

public sealed class JwtTokenService(
    IOptions<AuthSettings> authOptions,
    UserManager<ApplicationUser> userManager)
{
    public async Task<(string AccessToken, DateTime ExpiresAtUtc)> CreateTokenAsync(
        ApplicationUser user,
        CancellationToken ct = default)
    {
        var settings = authOptions.Value;
        var secret = settings.Jwt.SecretKey
            ?? throw new InvalidOperationException("Auth:Jwt:SecretKey must be configured.");

        var claims = new List<Claim>
        {
            new(settings.Claims.TenantId, user.TenantId.ToString()),
            new(settings.Claims.UserId, user.Id.ToString()),
            new(settings.Claims.UserName, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(settings.Jwt.Authority) ? "LiveSync" : settings.Jwt.Authority,
            audience: string.IsNullOrWhiteSpace(settings.Jwt.Audience) ? "LiveSync" : settings.Jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

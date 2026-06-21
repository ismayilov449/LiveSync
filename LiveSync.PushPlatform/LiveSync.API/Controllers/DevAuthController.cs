using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LiveSync.Application.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LiveSync.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("dev/auth")]
public sealed class DevAuthController(
    IOptions<AuthSettings> authOptions,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("token")]
    public ActionResult<DevTokenResponse> CreateToken([FromBody] DevTokenRequest request)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var settings = authOptions.Value;
        var secret = settings.Jwt.SecretKey;
        if (string.IsNullOrWhiteSpace(secret))
            return BadRequest("Auth:Jwt:SecretKey must be configured to mint development tokens.");

        var claims = new List<Claim>
        {
            new(settings.Claims.TenantId, request.TenantId.ToString()),
            new(settings.Claims.UserId, request.UserId.ToString()),
            new(settings.Claims.UserName, request.UserName),
            new(ClaimTypes.Name, request.UserName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(settings.Jwt.Authority) ? "LiveSync" : settings.Jwt.Authority,
            audience: string.IsNullOrWhiteSpace(settings.Jwt.Audience) ? "LiveSync" : settings.Jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return Ok(new DevTokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires
        });
    }
}

public sealed record DevTokenRequest(int TenantId = 1, int UserId = 1, string UserName = "dev-user");
public sealed record DevTokenResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}

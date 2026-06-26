using Asp.Versioning;
using LiveSync.API.Extensions;
using LiveSync.API.Identity;
using LiveSync.Application.Common.Constants;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Identity;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITenantProvisioner tenantProvisioner,
    JwtTokenService jwtTokenService,
    MasterDbContext masterDb,
    IHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(request.UserName)
            ?? await userManager.FindByEmailAsync(request.UserName);

        if (user is null)
            return Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized();

        var token = await jwtTokenService.CreateTokenAsync(user, ct);
        return Ok(new AuthTokenResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            TenantId = user.TenantId,
            UserId = user.Id,
            UserName = user.UserName ?? user.Email ?? string.Empty
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [HttpPost("register")]
    public async Task<ActionResult<AuthTokenResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName))
            return BadRequest("Tenant name is required.");

        var tenant = await tenantProvisioner.ProvisionTenantAsync(request.TenantName.Trim(), ct);

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            TenantId = tenant.Id
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        await IdentityRoleSeeder.AssignTenantAdminAsync(userManager, user, ct);

        var token = await jwtTokenService.CreateTokenAsync(user, ct);
        return Ok(new AuthTokenResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            TenantId = user.TenantId,
            UserId = user.Id,
            UserName = user.UserName ?? user.Email ?? string.Empty
        });
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<TenantUserResponse>>> ListUsers(
        IUserContext userContext,
        CancellationToken ct)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Where(x => x.TenantId == userContext.TenantId)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.UserName)
            .Select(x => new TenantUserResponse
            {
                UserId = x.Id,
                UserName = x.UserName ?? string.Empty,
                Email = x.Email ?? string.Empty,
                DisplayName = x.DisplayName
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    [Authorize(Roles = TenantRoles.TenantAdmin)]
    [HttpPost("users")]
    public async Task<ActionResult<CreatedUserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        IUserContext userContext,
        CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            TenantId = userContext.TenantId
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        await IdentityRoleSeeder.AssignTenantUserAsync(userManager, user, ct);

        return CreatedAtAction(
            nameof(GetProfile),
            new CreatedUserResponse
            {
                UserId = user.Id,
                TenantId = user.TenantId,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName
            });
    }

    [AllowAnonymous]
    [HttpPost("dev/users")]
    public async Task<ActionResult<CreatedUserResponse>> CreateUserForTenantDev(
        [FromBody] CreateUserForTenantRequest request,
        CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        if (request.TenantId <= 0)
            return BadRequest("TenantId must be greater than zero.");

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            TenantId = request.TenantId
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        if (request.AssignAdminRole)
            await IdentityRoleSeeder.AssignTenantAdminAsync(userManager, user, ct);
        else
            await IdentityRoleSeeder.AssignTenantUserAsync(userManager, user, ct);

        return Ok(new CreatedUserResponse
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(
        IUserContext userContext,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userContext.UserId.ToString());
        if (user is null)
            return NotFound();

        var roles = await userManager.GetRolesAsync(user);

        var tenant = await masterDb.Tenants
            .AsNoTracking()
            .Where(x => x.Id == user.TenantId)
            .Select(x => new { x.Name, x.Status })
            .FirstOrDefaultAsync(ct);

        return Ok(new UserProfileResponse
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            TenantName = tenant?.Name ?? string.Empty,
            TenantStatus = tenant?.Status.ToString() ?? nameof(TenantStatus.Active),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToList()
        });
    }
}

public sealed record LoginRequest(string UserName, string Password);
public sealed record RegisterRequest(
    string TenantName,
    string UserName,
    string Email,
    string Password,
    string DisplayName);

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    string DisplayName);

public sealed record CreateUserForTenantRequest(
    int TenantId,
    string UserName,
    string Email,
    string Password,
    string DisplayName,
    bool AssignAdminRole = false);

public sealed record AuthTokenResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public required int TenantId { get; init; }
    public required int UserId { get; init; }
    public required string UserName { get; init; }
}

public sealed record UserProfileResponse
{
    public required int UserId { get; init; }
    public required int TenantId { get; init; }
    public required string TenantName { get; init; }
    public required string TenantStatus { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}

public sealed record CreatedUserResponse
{
    public required int UserId { get; init; }
    public required int TenantId { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
}

public sealed record TenantUserResponse
{
    public required int UserId { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
}

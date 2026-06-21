using LiveSync.Application.Common.Constants;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.Identity;

public static class IdentityRoleSeeder
{
    public static async Task EnsureRolesAsync(MasterDbContext masterDb, CancellationToken ct = default)
    {
        await EnsureRoleAsync(masterDb, TenantRoles.TenantAdmin, ct);
        await EnsureRoleAsync(masterDb, TenantRoles.TenantUser, ct);
    }

    public static async Task EnsureRolesAsync(RoleManager<IdentityRole<int>> roleManager, CancellationToken ct = default)
    {
        await EnsureRoleAsync(roleManager, TenantRoles.TenantAdmin, ct);
        await EnsureRoleAsync(roleManager, TenantRoles.TenantUser, ct);
    }

    public static Task AssignTenantAdminAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        CancellationToken ct = default)
        => userManager.AddToRoleAsync(user, TenantRoles.TenantAdmin);

    public static Task AssignTenantUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        CancellationToken ct = default)
        => userManager.AddToRoleAsync(user, TenantRoles.TenantUser);

    private static async Task EnsureRoleAsync(
        MasterDbContext masterDb,
        string roleName,
        CancellationToken ct)
    {
        var normalizedName = NormalizeRoleName(roleName);
        if (await masterDb.Roles.AnyAsync(r => r.NormalizedName == normalizedName, ct))
            return;

        masterDb.Roles.Add(new IdentityRole<int>
        {
            Name = roleName,
            NormalizedName = normalizedName,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });
        await masterDb.SaveChangesAsync(ct);
    }

    private static string NormalizeRoleName(string roleName)
        => roleName.ToUpperInvariant();

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole<int>> roleManager,
        string roleName,
        CancellationToken ct)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return;

        var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
        }
    }
}

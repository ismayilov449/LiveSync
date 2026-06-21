using LiveSync.Application.Common.Interfaces;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSync.Infrastructure;

public static class ControlPlaneDependencyInjection
{
    public static IServiceCollection AddLiveSyncControlPlaneIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<MasterDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}

using System.Text;
using LiveSync.Application.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LiveSync.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddLiveSyncAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authSettings = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveToken = true;

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };

                if (!string.IsNullOrWhiteSpace(authSettings.Jwt.SecretKey))
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = authSettings.Jwt.ValidateIssuer,
                        ValidateAudience = authSettings.Jwt.ValidateAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = string.IsNullOrWhiteSpace(authSettings.Jwt.Authority)
                            ? "LiveSync"
                            : authSettings.Jwt.Authority,
                        ValidAudience = string.IsNullOrWhiteSpace(authSettings.Jwt.Audience)
                            ? "LiveSync"
                            : authSettings.Jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(authSettings.Jwt.SecretKey))
                    };
                }
                else if (!string.IsNullOrWhiteSpace(authSettings.Jwt.Authority))
                {
                    options.Authority = authSettings.Jwt.Authority;
                    options.Audience = authSettings.Jwt.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = authSettings.Jwt.ValidateIssuer,
                        ValidateAudience = authSettings.Jwt.ValidateAudience,
                        ValidateLifetime = true
                    };
                }
            });

        services.AddAuthorization();

        return services;
    }
}

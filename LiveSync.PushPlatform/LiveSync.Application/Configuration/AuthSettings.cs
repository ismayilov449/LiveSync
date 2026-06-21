namespace LiveSync.Application.Configuration;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    public string? ApiKey { get; set; }
    public bool RequireAuthentication { get; set; } = true;
    public JwtSettings Jwt { get; set; } = new();
    public ClaimSettings Claims { get; set; } = new();
    public DevelopmentAuthSettings Development { get; set; } = new();
}

public sealed class JwtSettings
{
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string? SecretKey { get; set; }
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
}

public sealed class ClaimSettings
{
    public string TenantId { get; set; } = "tenant_id";
    public string UserId { get; set; } = "user_id";
    public string UserName { get; set; } = "preferred_username";
}

public sealed class DevelopmentAuthSettings
{
    public bool AllowAnonymous { get; set; } = true;
    public bool AllowHeaderFallback { get; set; } = true;
    public int DefaultTenantId { get; set; } = 1;
    public int DefaultUserId { get; set; } = 1;
    public string DefaultUserName { get; set; } = "dev-user";
}

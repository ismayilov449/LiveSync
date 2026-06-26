using LiveSync.Application;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Configuration;
using LiveSync.Application.Hubs;
using LiveSync.API.Extensions;
using LiveSync.API.Identity;
using LiveSync.API.Middleware;
using LiveSync.API.Services;
using LiveSync.Infrastructure;
using LiveSync.Infrastructure.Extensions;
using LiveSync.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddLiveSyncObservability();

builder.Services.AddApplication();
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));
builder.Services.Configure<TenancySettings>(builder.Configuration.GetSection(TenancySettings.SectionName));

builder.Services.AddInfrastructure(builder.Configuration, options =>
{
    var applyMigrations = builder.Environment.IsDevelopment()
        || builder.Environment.IsEnvironment("Testing")
        || builder.Configuration.GetValue<bool>("Hosting:ApplyMigrationsOnStartup");

    options.RunChangeDetection = builder.Configuration.GetValue<bool>("Hosting:RunChangeDetection");
    options.RunSubscriptionExpiry = builder.Configuration.GetValue<bool>("Hosting:RunSubscriptionExpiry");
    options.ApplyControlPlaneMigrationsOnStartup = applyMigrations;
    options.MigrateFromSharedDatabaseOnStartup = builder.Configuration
        .GetSection(TenancySettings.SectionName)
        .GetValue<bool>("MigrateFromSharedDatabase");
    options.MigrateTenantDatabasesOnStartup = applyMigrations;
    options.SeedDataOnStartup = builder.Environment.IsDevelopment();
});

builder.Services.AddLiveSyncControlPlaneIdentity(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityUserAccessor, HttpIdentityUserAccessor>();
builder.Services.AddScoped<IUserContext, HttpUserContext>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddLiveSyncAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddLiveSyncApiVersioning();
builder.Services.AddLiveSyncRateLimiting();

builder.Services.AddLiveSyncSignalR(builder.Configuration);
builder.Services.AddControllers();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5252", "http://localhost:5173"];

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddLiveSyncHealthChecks(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
    };
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await DatabaseInitializer.InitializeAsync(app.Services);

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();

if (app.Environment.IsDevelopment())
    app.UseMiddleware<DevelopmentHeaderAuthenticationMiddleware>();

app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<TenantStatusMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapLiveSyncHealthChecks();
app.MapLiveSyncObservabilityEndpoints();
app.MapControllers();
app.MapHub<PushHub>("/hubs/push");

app.MapFallback(async context =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var indexPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "index.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
});

app.Run();

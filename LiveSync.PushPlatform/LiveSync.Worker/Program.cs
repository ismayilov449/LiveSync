using LiveSync.Application;
using LiveSync.Infrastructure;
using LiveSync.Infrastructure.Extensions;
using LiveSync.Infrastructure.Persistence;
using LiveSync.Infrastructure.Persistence.ControlPlane;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, options =>
{
    options.RunChangeDetection = true;
    options.RunSubscriptionExpiry = true;
    options.ApplyControlPlaneMigrationsOnStartup = builder.Environment.IsDevelopment();
    options.MigrateFromSharedDatabaseOnStartup = builder.Configuration
        .GetSection(LiveSync.Application.Configuration.TenancySettings.SectionName)
        .GetValue<bool>("MigrateFromSharedDatabase");
    options.MigrateTenantDatabasesOnStartup = builder.Environment.IsDevelopment();
    options.SeedDataOnStartup = false;
});

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

builder.Services.AddLiveSyncSignalR(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MasterDbContext>("control-plane", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);

var app = builder.Build();

app.UseSerilogRequestLogging();

await DatabaseInitializer.InitializeAsync(app.Services);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, _) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { status = "Alive" });
    }
});

app.Run();

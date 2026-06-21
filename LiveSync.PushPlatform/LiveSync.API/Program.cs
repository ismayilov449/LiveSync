using LiveSync.Application;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.Hubs;
using LiveSync.API.Services;
using LiveSync.Infrastructure;
using LiveSync.Infrastructure.Extensions;
using LiveSync.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

builder.Services.AddLiveSyncSignalR(builder.Configuration);
builder.Services.AddControllers();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5252"];

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("sql")
    .AddRedis(redisConnectionString, name: "redis");

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<PushHub>("/hubs/push");

app.Run();

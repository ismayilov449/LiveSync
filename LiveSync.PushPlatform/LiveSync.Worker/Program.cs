using LiveSync.Application;
using LiveSync.Infrastructure;
using LiveSync.Infrastructure.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

builder.Services.AddLiveSyncSignalR(builder.Configuration);
var host = builder.Build();
host.Run();
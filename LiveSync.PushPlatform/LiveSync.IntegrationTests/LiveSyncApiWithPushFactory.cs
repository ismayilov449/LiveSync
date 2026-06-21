using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace LiveSync.IntegrationTests;

public sealed class LiveSyncApiWithPushFactory : LiveSyncApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hosting:RunChangeDetection"] = "true",
                ["ChangeDetection:Enabled"] = "true",
                ["ChangeDetection:PollIntervalMs"] = "250",
            });
        });
    }
}

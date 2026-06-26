using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LiveSync.IntegrationTests;

public sealed class LiveSyncApiWithPushFactory : LiveSyncApiFactory
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hosting:RunChangeDetection"] = "true",
                ["ChangeDetection:Enabled"] = "true",
                ["ChangeDetection:PollIntervalMs"] = "250",
            });
        });

        return base.CreateHost(builder);
    }
}

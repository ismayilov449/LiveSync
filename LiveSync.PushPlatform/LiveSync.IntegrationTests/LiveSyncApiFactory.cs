using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace LiveSync.IntegrationTests;

public class LiveSyncApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ControlPlaneDatabase = "LiveSync_ControlPlane";
    private const string JwtSecretKey = "integration-test-secret-key-min-32-chars";

    private readonly MsSqlContainer _msSql = new MsSqlBuilder().Build();
    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public async Task InitializeAsync()
    {
        await _msSql.StartAsync();
        await _redis.StartAsync();
        await EnsureControlPlaneDatabaseAsync();
    }

    private async Task EnsureControlPlaneDatabaseAsync()
    {
        await using var connection = new SqlConnection(_msSql.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF DB_ID(@databaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE [' + REPLACE(@databaseName, ']', ']]') + N']';
                EXEC sp_executesql @sql;
            END
            """;
        command.Parameters.AddWithValue("@databaseName", ControlPlaneDatabase);
        await command.ExecuteNonQueryAsync();
    }

    public new async Task DisposeAsync()
    {
        await _msSql.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var controlPlaneConnectionString = BuildControlPlaneConnectionString();
            var tenantConnectionTemplate = BuildTenantConnectionTemplate();

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ControlPlane"] = controlPlaneConnectionString,
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["Tenancy:ConnectionTemplate"] = tenantConnectionTemplate,
                ["Auth:Jwt:SecretKey"] = JwtSecretKey,
                ["Hosting:ApplyMigrationsOnStartup"] = "true",
            });
        });
    }

    private string BuildControlPlaneConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_msSql.GetConnectionString())
        {
            InitialCatalog = ControlPlaneDatabase
        };
        return builder.ConnectionString;
    }

    private string BuildTenantConnectionTemplate()
    {
        var builder = new SqlConnectionStringBuilder(_msSql.GetConnectionString())
        {
            InitialCatalog = "{DatabaseName}"
        };
        return builder.ConnectionString;
    }
}

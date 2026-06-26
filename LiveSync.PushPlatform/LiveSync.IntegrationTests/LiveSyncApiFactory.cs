using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace LiveSync.IntegrationTests;

public class LiveSyncApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ControlPlaneDatabase = "LiveSync_ControlPlane";
    private const string JwtSecretKey = "integration-test-secret-key-min-32-chars";

    private readonly MsSqlContainer _msSql = new MsSqlBuilder().Build();
    private readonly RedisContainer _redis = new RedisBuilder().Build();
    private bool _containersStarted;

    public async Task InitializeAsync()
    {
        await _msSql.StartAsync();
        await _redis.StartAsync();
        await EnsureControlPlaneDatabaseAsync();
        _containersStarted = true;
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
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (!_containersStarted)
        {
            throw new InvalidOperationException(
                "Integration test containers must be started before creating the test host.");
        }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(BuildTestConfiguration());
        });

        return base.CreateHost(builder);
    }

    protected virtual Dictionary<string, string?> BuildTestConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:ControlPlane"] = BuildControlPlaneConnectionString(),
            ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
            ["Tenancy:ConnectionTemplate"] = BuildTenantConnectionTemplate(),
            ["Auth:Jwt:SecretKey"] = JwtSecretKey,
            ["Hosting:ApplyMigrationsOnStartup"] = "true",
        };
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

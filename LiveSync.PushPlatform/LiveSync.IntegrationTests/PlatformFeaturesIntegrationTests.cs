using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.API.Contracts.Items;
using LiveSync.Application.CQRS.Items.Models;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class PlatformFeaturesIntegrationTests(LiveSyncApiFactory factory)
{
    private async Task<(HttpClient AdminClient, int RootId)> CreateTenantAdminAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                TenantName: $"Platform-{suffix}",
                UserName: $"admin-{suffix}",
                Email: $"admin-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Admin"));

        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        auth.Should().NotBeNull();

        var adminClient = factory.CreateAuthenticatedClient(auth!.AccessToken);
        var list = await adminClient.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items?page=1&pageSize=20");
        list.Should().NotBeNull();
        var rootId = list!.Items.OrderBy(x => x.Id).First().Id;
        return (adminClient, rootId);
    }

    [Fact]
    public async Task TenantAdmin_CanSuspendAndReactivateTenant()
    {
        var (adminClient, rootId) = await CreateTenantAdminAsync();

        var createBeforeSuspend = await adminClient.PostAsJsonAsync(
            "/api/v1/items",
            new CreateItemRequest(rootId, "before suspend"));
        createBeforeSuspend.StatusCode.Should().Be(HttpStatusCode.Created);

        var suspendResponse = await adminClient.PostAsync("/api/v1/tenants/suspend", null);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var blockedResponse = await adminClient.GetAsync("/api/v1/items?page=1&pageSize=20");
        blockedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var reactivateResponse = await adminClient.PostAsync("/api/v1/tenants/reactivate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterReactivate = await adminClient.GetAsync("/api/v1/items?page=1&pageSize=20");
        afterReactivate.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateItem_WithIdempotencyKey_ReturnsSameIdOnReplay()
    {
        var (adminClient, rootId) = await CreateTenantAdminAsync();
        var key = Guid.NewGuid().ToString("N");

        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/items")
        {
            Content = JsonContent.Create(new CreateItemRequest(rootId, "idempotent item"))
        };
        request1.Headers.Add("Idempotency-Key", key);

        var response1 = await adminClient.SendAsync(request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        var id1 = await response1.Content.ReadFromJsonAsync<int>();

        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/items")
        {
            Content = JsonContent.Create(new CreateItemRequest(rootId, "different payload"))
        };
        request2.Headers.Add("Idempotency-Key", key);

        var response2 = await adminClient.SendAsync(request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        var id2 = await response2.Content.ReadFromJsonAsync<int>();

        id2.Should().Be(id1);
    }

    [Fact]
    public async Task CreateItem_WritesAuditLogEntry()
    {
        var (adminClient, rootId) = await CreateTenantAdminAsync();

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/items",
            new CreateItemRequest(rootId, "audited item"));
        createResponse.EnsureSuccessStatusCode();

        var audit = await adminClient.GetFromJsonAsync<AuditListResponse>("/api/v1/audit?page=1&pageSize=10");
        audit.Should().NotBeNull();
        audit!.Items.Should().Contain(x =>
            x.Action == "create" && x.EntityType == "item" && x.Details == "audited item");
    }

    [Fact]
    public async Task MetricsEndpoint_IsAvailable()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("livesync");
    }

    private sealed record AuditListResponse(
        IReadOnlyList<AuditItem> Items,
        int Page,
        int PageSize,
        int TotalCount);

    private sealed record AuditItem(
        long Id,
        int TenantId,
        int UserId,
        string Action,
        string EntityType,
        string? EntityId,
        string? Details,
        DateTime CreatedAtUtc);
}

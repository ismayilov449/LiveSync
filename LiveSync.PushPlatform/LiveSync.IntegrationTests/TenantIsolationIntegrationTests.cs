using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.API.Contracts.Items;
using LiveSync.Application.CQRS.Items.Models;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class TenantIsolationIntegrationTests(LiveSyncApiFactory factory)
{
    [Fact]
    public async Task ItemsCreatedInOneTenant_AreNotVisibleInAnotherTenant()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var tenant1 = await RegisterAsync(
            factory,
            $"TenantA-{suffix}",
            $"user-a-{suffix}",
            $"user-a-{suffix}@test.local");

        var tenant2 = await RegisterAsync(
            factory,
            $"TenantB-{suffix}",
            $"user-b-{suffix}",
            $"user-b-{suffix}@test.local");

        var tenant1Client = factory.CreateAuthenticatedClient(tenant1.Token);
        var tenant2Client = factory.CreateAuthenticatedClient(tenant2.Token);

        var tenant1RootId = await GetRootItemIdAsync(tenant1Client);
        var tenant2RootId = await GetRootItemIdAsync(tenant2Client);

        var createResponse = await tenant1Client.PostAsJsonAsync(
            "/api/v1/items",
            new CreateItemRequest(tenant1RootId, "Tenant1 Only Item"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdId = await createResponse.Content.ReadFromJsonAsync<int>();
        createdId.Should().BeGreaterThan(0);

        var tenant2List = await tenant2Client.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items");
        tenant2List.Should().NotBeNull();
        tenant2List!.Items.Should().NotContain(x => x.Id == createdId);
        tenant2List.Items.Should().NotContain(x => x.Name == "Tenant1 Only Item");

        var tenant1List = await tenant1Client.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items");
        tenant1List.Should().NotBeNull();
        tenant1List!.Items.Should().Contain(x => x.Id == createdId && x.Name == "Tenant1 Only Item");
        tenant1.TenantId.Should().NotBe(tenant2.TenantId);
    }

    private static async Task<(string Token, int TenantId)> RegisterAsync(
        LiveSyncApiFactory factory,
        string tenantName,
        string userName,
        string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(tenantName, userName, email, "Password1", userName));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        return (body!.AccessToken, body.TenantId);
    }

    private static async Task<int> GetRootItemIdAsync(HttpClient client)
    {
        var list = await client.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items");
        list.Should().NotBeNull();
        list!.Items.Should().NotBeEmpty();
        return list.Items.OrderBy(x => x.Id).First().Id;
    }
}

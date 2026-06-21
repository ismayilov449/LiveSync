using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.API.Contracts.Items;
using LiveSync.Application.CQRS.Items.Models;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class ItemsIntegrationTests(LiveSyncApiFactory factory)
{
    [Fact]
    public async Task CreateItem_ReturnsId_AndAppearsInPaginatedList()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                TenantName: $"ItemsTenant-{suffix}",
                UserName: $"items-{suffix}",
                Email: $"items-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Items User"));

        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        auth.Should().NotBeNull();

        var authedClient = factory.CreateAuthenticatedClient(auth!.AccessToken);

        var listBefore = await authedClient.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items?page=1&pageSize=20");
        listBefore.Should().NotBeNull();

        var rootId = listBefore!.Items.OrderBy(x => x.Id).First().Id;
        const string itemName = "Integration Test Item";

        var createResponse = await authedClient.PostAsJsonAsync(
            "/api/v1/items",
            new CreateItemRequest(rootId, itemName));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdId = await createResponse.Content.ReadFromJsonAsync<int>();
        createdId.Should().BeGreaterThan(0);

        var listAfter = await authedClient.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items?page=1&pageSize=20");
        listAfter.Should().NotBeNull();
        listAfter!.TotalCount.Should().BeGreaterThan(listBefore.TotalCount);
        listAfter.Items.Should().Contain(x => x.Id == createdId && x.Name == itemName);
    }
}

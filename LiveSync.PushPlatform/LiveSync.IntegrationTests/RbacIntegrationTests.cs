using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.API.Contracts.Items;
using LiveSync.Application.CQRS.Items.Models;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class RbacIntegrationTests(LiveSyncApiFactory factory)
{
    [Fact]
    public async Task TenantUser_CannotDeleteItems_ButTenantAdminCan()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                TenantName: $"RbacTenant-{suffix}",
                UserName: $"admin-{suffix}",
                Email: $"admin-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Admin User"));

        registerResponse.EnsureSuccessStatusCode();
        var adminAuth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        adminAuth.Should().NotBeNull();

        var adminClient = factory.CreateAuthenticatedClient(adminAuth!.AccessToken);

        var inviteResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/auth/users",
            new CreateUserRequest(
                UserName: $"user-{suffix}",
                Email: $"user-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Regular User"));

        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var userLoginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest($"user-{suffix}", "Password1"));

        userLoginResponse.EnsureSuccessStatusCode();
        var userAuth = await userLoginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        userAuth.Should().NotBeNull();

        var userClient = factory.CreateAuthenticatedClient(userAuth!.AccessToken);

        var list = await adminClient.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items?page=1&pageSize=20");
        list.Should().NotBeNull();
        var rootId = list!.Items.OrderBy(x => x.Id).First().Id;

        var createResponse = await userClient.PostAsJsonAsync(
            "/api/v1/items",
            new CreateItemRequest(rootId, "RBAC test item"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var itemId = await createResponse.Content.ReadFromJsonAsync<int>();
        itemId.Should().BeGreaterThan(0);

        var userDeleteResponse = await userClient.DeleteAsync($"/api/v1/items/{itemId}");
        userDeleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var adminDeleteResponse = await adminClient.DeleteAsync($"/api/v1/items/{itemId}");
        adminDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TenantUser_CannotInviteUsers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                TenantName: $"InviteTenant-{suffix}",
                UserName: $"admin-{suffix}",
                Email: $"admin-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Admin User"));

        registerResponse.EnsureSuccessStatusCode();
        var adminAuth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

        var adminClient = factory.CreateAuthenticatedClient(adminAuth!.AccessToken);

        await adminClient.PostAsJsonAsync(
            "/api/v1/auth/users",
            new CreateUserRequest(
                UserName: $"member-{suffix}",
                Email: $"member-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Member User"));

        var userLoginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest($"member-{suffix}", "Password1"));

        userLoginResponse.EnsureSuccessStatusCode();
        var userAuth = await userLoginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        var userClient = factory.CreateAuthenticatedClient(userAuth!.AccessToken);

        var inviteResponse = await userClient.PostAsJsonAsync(
            "/api/v1/auth/users",
            new CreateUserRequest(
                UserName: $"blocked-{suffix}",
                Email: $"blocked-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Blocked User"));

        inviteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

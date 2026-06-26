using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class RbacIntegrationTests(LiveSyncApiFactory factory)
{
    [Fact]
    public async Task TenantUser_CannotDeleteTickets_ButTenantAdminCan()
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

        var queueId = await IntegrationTestHelpers.GetDefaultQueueIdAsync(adminClient);

        var createResponse = await userClient.PostAsJsonAsync(
            "/api/v1/tickets",
            IntegrationTestHelpers.SampleTicket(queueId, userAuth.UserId, "RBAC test ticket"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticketId = await createResponse.Content.ReadFromJsonAsync<int>();
        ticketId.Should().BeGreaterThan(0);

        var userDeleteResponse = await userClient.DeleteAsync($"/api/v1/tickets/{ticketId}");
        userDeleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var adminDeleteResponse = await adminClient.DeleteAsync($"/api/v1/tickets/{ticketId}");
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

    [Fact]
    public async Task AuthenticatedUser_CanListUsersInOwnTenantOnly()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                TenantName: $"UsersTenant-{suffix}",
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
                UserName: $"agent-{suffix}",
                Email: $"agent-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Support Agent"));

        var listResponse = await adminClient.GetAsync("/api/v1/auth/users");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await listResponse.Content.ReadFromJsonAsync<List<TenantUserResponse>>();
        users.Should().NotBeNull();
        users!.Should().HaveCount(2);
        users.Should().Contain(x => x.DisplayName == "Admin User");
        users.Should().Contain(x => x.DisplayName == "Support Agent");
    }
}

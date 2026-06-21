using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class AuthIntegrationTests(LiveSyncApiFactory factory)
{
    [Fact]
    public async Task Register_ReturnsTokenWithTenantId()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new RegisterRequest(
            TenantName: $"Tenant-{suffix}",
            UserName: $"user-{suffix}",
            Email: $"user-{suffix}@test.local",
            Password: "Password1",
            DisplayName: "Test User");

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.TenantId.Should().BeGreaterThan(0);
        body.UserId.Should().BeGreaterThan(0);
        body.UserName.Should().Be(request.UserName);
    }

    [Fact]
    public async Task Login_ReturnsTokenForRegisteredUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registerRequest = new RegisterRequest(
            TenantName: $"Tenant-{suffix}",
            UserName: $"login-{suffix}",
            Email: $"login-{suffix}@test.local",
            Password: "Password1",
            DisplayName: "Login User");

        var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(registerRequest.UserName, registerRequest.Password));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.TenantId.Should().BeGreaterThan(0);
        body.UserName.Should().Be(registerRequest.UserName);
    }
}

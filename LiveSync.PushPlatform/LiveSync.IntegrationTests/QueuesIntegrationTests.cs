using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.API.Contracts.Queues;
using LiveSync.Application.CQRS.Queues.Models;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class QueuesIntegrationTests(LiveSyncApiFactory factory)
{
  [Fact]
  public async Task CreateQueue_ReturnsId_AndAppearsInPaginatedList()
  {
    var suffix = Guid.NewGuid().ToString("N")[..8];
    var client = factory.CreateClient();

    var registerResponse = await client.PostAsJsonAsync(
      "/api/v1/auth/register",
      new RegisterRequest(
        TenantName: $"QueuesTenant-{suffix}",
        UserName: $"queues-{suffix}",
        Email: $"queues-{suffix}@test.local",
        Password: "Password1",
        DisplayName: "Queues User"));

    registerResponse.EnsureSuccessStatusCode();
    var auth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
    auth.Should().NotBeNull();

    var authedClient = factory.CreateAuthenticatedClient(auth!.AccessToken);

    var listBefore = await authedClient.GetFromJsonAsync<PagedQueuesResponse>("/api/v1/queues?page=1&pageSize=20");
    listBefore.Should().NotBeNull();

    const string queueName = "Integration Test Queue";

    var createResponse = await authedClient.PostAsJsonAsync(
      "/api/v1/queues",
      new CreateQueueRequest(queueName));

    createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    var createdId = await createResponse.Content.ReadFromJsonAsync<int>();
    createdId.Should().BeGreaterThan(0);

    var listAfter = await authedClient.GetFromJsonAsync<PagedQueuesResponse>("/api/v1/queues?page=1&pageSize=20");
    listAfter.Should().NotBeNull();
    listAfter!.TotalCount.Should().BeGreaterThan(listBefore!.TotalCount);
    listAfter.Items.Should().Contain(x => x.Id == createdId && x.Name == queueName);
  }
}

[Collection("Integration")]
public sealed class QueuesTenantIsolationIntegrationTests(LiveSyncApiFactory factory)
{
  [Fact]
  public async Task QueuesCreatedInOneTenant_AreNotVisibleInAnotherTenant()
  {
    var suffix = Guid.NewGuid().ToString("N")[..8];

    var tenant1Client = factory.CreateAuthenticatedClient((await RegisterAsync(
      factory,
      $"TenantA-{suffix}",
      $"user-a-{suffix}",
      $"user-a-{suffix}@test.local")).Token);

    var tenant2Client = factory.CreateAuthenticatedClient((await RegisterAsync(
      factory,
      $"TenantB-{suffix}",
      $"user-b-{suffix}",
      $"user-b-{suffix}@test.local")).Token);

    var createResponse = await tenant1Client.PostAsJsonAsync(
      "/api/v1/queues",
      new CreateQueueRequest("Tenant1 Only Queue"));
    createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

    var createdId = await createResponse.Content.ReadFromJsonAsync<int>();
    createdId.Should().BeGreaterThan(0);

    var tenant2List = await tenant2Client.GetFromJsonAsync<PagedQueuesResponse>("/api/v1/queues");
    tenant2List.Should().NotBeNull();
    tenant2List!.Items.Should().NotContain(x => x.Id == createdId);
    tenant2List.Items.Should().NotContain(x => x.Name == "Tenant1 Only Queue");

    var tenant1List = await tenant1Client.GetFromJsonAsync<PagedQueuesResponse>("/api/v1/queues");
    tenant1List.Should().NotBeNull();
    tenant1List!.Items.Should().Contain(x => x.Id == createdId && x.Name == "Tenant1 Only Queue");
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
}

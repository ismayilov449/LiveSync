using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.Application.CQRS.Tickets.Models;
using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Application.RealTimeSync.Ports;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSync.IntegrationTests;

[Collection("Integration")]
public sealed class TransactionalOutboxIntegrationTests(LiveSyncApiFactory factory)
{
    [Fact]
    public async Task OpenTicket_IsRolledBack_WhenChangeQueueWriteFails()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registerResponse = await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest($"Outbox-{suffix}", $"outbox-{suffix}", $"outbox-{suffix}@test.local", "Password1", "Outbox User"));
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

        var client = factory.CreateAuthenticatedClient(auth!.AccessToken);
        var queueId = await IntegrationTestHelpers.GetDefaultQueueIdAsync(client);

        await using var failingOutbox = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IChangeQueueStore, FailingChangeQueueStore>()));
        var failingClient = failingOutbox.CreateClient();
        failingClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;

        var subject = $"Never committed {suffix}";
        var createResponse = await failingClient.PostAsJsonAsync(
            "/api/v1/tickets",
            IntegrationTestHelpers.SampleTicket(queueId, auth.UserId, subject));

        createResponse.IsSuccessStatusCode.Should().BeFalse();

        var tickets = await client.GetFromJsonAsync<PagedTicketsResponse>("/api/v1/tickets");
        tickets!.Items.Should().NotContain(x => x.Subject == subject,
            "the ticket and its change queue row must commit in the same transaction");
    }

    private sealed class FailingChangeQueueStore : IChangeQueueStore
    {
        public Task EnqueueAsync(ChangeEnvelope change, CancellationToken ct = default)
            => throw new InvalidOperationException("Simulated change queue failure.");

        public Task<IReadOnlyList<QueuedChange>> ClaimBatchAsync(string version, int batchSize, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<QueuedChange>>([]);

        public Task MarkProcessedAsync(long queueEntryId, CancellationToken ct = default) => Task.CompletedTask;

        public Task MarkFailedAsync(long queueEntryId, string error, CancellationToken ct = default) => Task.CompletedTask;

        public Task<ChangeQueueStatistics> GetStatisticsAsync(string version, CancellationToken ct = default)
            => Task.FromResult(new ChangeQueueStatistics(0, 0));
    }
}

using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.API.Contracts.Queues;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.RealTimeSync.Commands;
using LiveSync.Application.RealTimeSync.Contracts;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSync.IntegrationTests;

[Collection("IntegrationPush")]
public sealed class BucketScopedPushIntegrationTests(LiveSyncApiWithPushFactory factory)
{
    [Fact]
    public async Task OpenTicket_DoesNotPushToQueueSubscriber()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                TenantName: $"BucketTenant-{suffix}",
                UserName: $"bucket-{suffix}",
                Email: $"bucket-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Bucket User"));

        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        auth.Should().NotBeNull();

        var queuePushReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ticketPushReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(client.BaseAddress!, $"/hubs/push?access_token={Uri.EscapeDataString(auth!.AccessToken)}"),
                options => options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler())
            .WithAutomaticReconnect()
            .Build();

        connection.On<ChangeNotificationDto>("PushUpdate", notification =>
        {
            if (notification.Entity.Bucket == "queue")
                queuePushReceived.TrySetResult();
            if (notification.Entity.Bucket == "ticket")
                ticketPushReceived.TrySetResult();
        });

        await connection.StartAsync();

        await connection.InvokeAsync<SubscribeResponse>(
            "FindAndSubscribe",
            new { bucket = "Queue", filter = $"queue.TenantId == {auth.TenantId}" });

        var authedClient = factory.CreateAuthenticatedClient(auth.AccessToken);
        var queueId = await IntegrationTestHelpers.GetDefaultQueueIdAsync(authedClient);

        var createResponse = await authedClient.PostAsJsonAsync(
            "/api/v1/tickets",
            IntegrationTestHelpers.SampleTicket(queueId, auth.UserId, "Cross-bucket isolation test"));

        createResponse.EnsureSuccessStatusCode();

        for (var attempt = 0; attempt < 40 && !ticketPushReceived.Task.IsCompleted; attempt++)
        {
            using var scope = factory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenantId(auth.TenantId);
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new ProcessPendingChangesCommand());
            await Task.Delay(100);
        }

        ticketPushReceived.Task.IsCompleted.Should().BeFalse(
            "queue subscriber must not receive ticket bucket pushes when using bucket-scoped SignalR groups");

        var queueCreate = await authedClient.PostAsJsonAsync(
            "/api/v1/queues",
            new CreateQueueRequest("Queue push test"));

        queueCreate.EnsureSuccessStatusCode();

        for (var attempt = 0; attempt < 40 && !queuePushReceived.Task.IsCompleted; attempt++)
        {
            using var scope = factory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenantId(auth.TenantId);
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new ProcessPendingChangesCommand());
            await Task.Delay(100);
        }

        queuePushReceived.Task.IsCompleted.Should().BeTrue(
            "queue subscriber should receive queue bucket pushes");
    }

    private sealed record SubscribeResponse(string SubscriptionId);
}

using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Controllers;
using LiveSync.API.Contracts.Items;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Application.CQRS.Items.Models;
using LiveSync.Application.CQRS.RealTimeSync.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSync.IntegrationTests;

[Collection("IntegrationPush")]
public sealed class SignalRPushIntegrationTests(LiveSyncApiWithPushFactory factory)
{
    [Fact]
    public async Task CreateItem_TriggersPushUpdate_OnSubscribedClient()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                TenantName: $"PushTenant-{suffix}",
                UserName: $"push-{suffix}",
                Email: $"push-{suffix}@test.local",
                Password: "Password1",
                DisplayName: "Push User"));

        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        auth.Should().NotBeNull();

        var pushReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(client.BaseAddress!, $"/hubs/push?access_token={Uri.EscapeDataString(auth!.AccessToken)}"),
                options => options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler())
            .WithAutomaticReconnect()
            .Build();

        connection.On<object>("PushUpdate", _ => pushReceived.TrySetResult());

        await connection.StartAsync();

        var subscribeResponse = await connection.InvokeAsync<SubscribeResponse>(
            "FindAndSubscribe",
            new { bucket = "Item", filter = $"item.TenantId == {auth.TenantId}" });

        subscribeResponse.SubscriptionId.Should().NotBeNullOrWhiteSpace();

        var authedClient = factory.CreateAuthenticatedClient(auth.AccessToken);
        var list = await authedClient.GetFromJsonAsync<PagedItemsResponse>("/api/v1/items?page=1&pageSize=20");
        list.Should().NotBeNull();
        var rootId = list!.Items.OrderBy(x => x.Id).First().Id;

        var createResponse = await authedClient.PostAsJsonAsync(
            "/api/v1/items",
            new CreateItemRequest(rootId, "Push test item"));

        createResponse.EnsureSuccessStatusCode();

        for (var attempt = 0; attempt < 40 && !pushReceived.Task.IsCompleted; attempt++)
        {
            using var scope = factory.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenantId(auth.TenantId);
            await scope.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new ProcessPendingChangesCommand());

            await Task.Delay(100);
        }

        pushReceived.Task.IsCompleted.Should().BeTrue("expected PushUpdate after item creation and change processing");

        await connection.InvokeAsync("Unsubscribe", subscribeResponse.SubscriptionId);
    }

    private sealed record SubscribeResponse(string SubscriptionId);
}

[CollectionDefinition("IntegrationPush", DisableParallelization = true)]
public sealed class IntegrationPushCollection : ICollectionFixture<LiveSyncApiWithPushFactory>;

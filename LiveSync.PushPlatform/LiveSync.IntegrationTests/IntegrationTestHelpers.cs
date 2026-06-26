using System.Net.Http.Json;
using FluentAssertions;
using LiveSync.API.Contracts.Tickets;
using LiveSync.Application.CQRS.Queues.Models;
using LiveSync.Domain.Enums;

namespace LiveSync.IntegrationTests;

internal static class IntegrationTestHelpers
{
    public static async Task<int> GetDefaultQueueIdAsync(HttpClient client)
    {
        var list = await client.GetFromJsonAsync<PagedQueuesResponse>("/api/v1/queues");
        list.Should().NotBeNull();
        list!.Items.Should().NotBeEmpty();
        return list.Items.OrderBy(x => x.Id).First().Id;
    }

    public static OpenTicketRequest SampleTicket(int queueId, int reporterUserId, string subject)
        => new(queueId, subject, "Integration test ticket", TicketPriority.Normal, reporterUserId);
}

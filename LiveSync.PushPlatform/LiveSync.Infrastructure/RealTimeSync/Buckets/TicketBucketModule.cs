using System.Text.Json;
using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using LiveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSync.Infrastructure.RealTimeSync.Buckets;

public sealed class TicketBucketModule(AppDbContext db) : IBucketModule
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TopicBucket Bucket => TopicBucket.Ticket;
    public Type DtoClrType => typeof(TicketCacheDto);
    public string FilterParameterName => "ticket";

    public async Task<ICacheDto?> FetchDtoAsync(int tenantId, int id, CancellationToken ct = default)
        => await db.Tickets
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new TicketCacheDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                QueueId = x.QueueId,
                Subject = x.Subject,
                Status = x.Status,
                Priority = x.Priority,
                AssigneeUserId = x.AssigneeUserId,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ICacheDto>> FetchAllActiveAsync(int tenantId, CancellationToken ct = default)
    {
        var tickets = await db.Tickets
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .Select(x => new TicketCacheDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                QueueId = x.QueueId,
                Subject = x.Subject,
                Status = x.Status,
                Priority = x.Priority,
                AssigneeUserId = x.AssigneeUserId,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return tickets.Cast<ICacheDto>().ToList();
    }

    public ICacheDto? Deserialize(string json)
        => JsonSerializer.Deserialize<TicketCacheDto>(json, JsonOptions);
}

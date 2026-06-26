namespace LiveSync.Application.Common.Interfaces;

public sealed record AuditEventDto(
    long Id,
    int TenantId,
    int UserId,
    string Action,
    string EntityType,
    string? EntityId,
    string? Details,
    DateTime CreatedAtUtc);

public sealed record PagedAuditEvents(
    IReadOnlyList<AuditEventDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface IAuditService
{
    Task RecordAsync(
        int tenantId,
        int userId,
        string action,
        string entityType,
        string? entityId,
        string? details,
        CancellationToken ct = default);

    Task<PagedAuditEvents> ListAsync(int tenantId, int page, int pageSize, CancellationToken ct = default);
}

namespace LiveSync.Application.CQRS.Items.Models;

public sealed record ItemDto(
    int Id,
    int TenantId,
    int ParentId,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc);

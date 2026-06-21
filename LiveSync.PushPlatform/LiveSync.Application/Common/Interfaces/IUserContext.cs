namespace LiveSync.Application.Common.Interfaces;

public interface IUserContext
{
    int TenantId { get; }
    int UserId { get; }
}

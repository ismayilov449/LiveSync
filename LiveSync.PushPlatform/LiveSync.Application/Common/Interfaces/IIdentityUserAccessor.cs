namespace LiveSync.Application.Common.Interfaces;

public interface IIdentityUserAccessor
{
    IIdentityUser Current { get; }
}

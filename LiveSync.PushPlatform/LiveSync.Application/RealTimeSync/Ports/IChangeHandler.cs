using LiveSync.Application.RealTimeSync.Models;
using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface IChangeHandler
{
    TopicBucket Bucket { get; }
    Task HandleAsync(ChangeEnvelope change, CancellationToken ct = default);
}

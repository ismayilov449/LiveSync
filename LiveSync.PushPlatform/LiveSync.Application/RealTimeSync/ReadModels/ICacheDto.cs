using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.ReadModels;

public interface ICacheDto
{
    string FrontEndId { get; }
    TopicBucket Bucket { get; }
}

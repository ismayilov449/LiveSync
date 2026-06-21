using LiveSync.Domain.Enums;

namespace LiveSync.Application.RealTimeSync.Buckets;

public sealed class BucketModuleRegistry
{
    private readonly IReadOnlyDictionary<TopicBucket, IBucketModule> _modules;

    public BucketModuleRegistry(IEnumerable<IBucketModule> modules)
    {
        _modules = modules.ToDictionary(m => m.Bucket);
    }

    public IBucketModule GetRequired(TopicBucket bucket)
        => _modules.TryGetValue(bucket, out var module)
            ? module
            : throw new NotSupportedException($"Bucket '{bucket}' is not registered.");

    public IEnumerable<IBucketModule> GetAll() => _modules.Values;
}

using LiveSync.Domain.Common;
using LiveSync.Domain.Enums;

namespace LiveSync.Domain.ValueObjects;

public sealed class FrontEndId : ValueObject
{
    public string Value { get; }

    public FrontEndId(TopicBucket bucket, params int[] idComponents)
    {
        if (idComponents.Length == 0) throw new ArgumentException("At least one id component is required");

        Value = $"{bucket.ToString().ToLowerInvariant()}-{string.Join('-', idComponents)}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

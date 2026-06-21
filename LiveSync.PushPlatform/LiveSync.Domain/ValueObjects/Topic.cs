using LiveSync.Domain.Common;
using LiveSync.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace LiveSync.Domain.ValueObjects;

public sealed class Topic : ValueObject
{
    public int TenantId { get; }
    public TopicBucket Bucket { get; }
    public string Filter { get; }
    public string Key { get; }
    public string Hash { get; }

    public Topic(int tenantId, TopicBucket bucket, string filter)
    {
        if (tenantId <= 0) throw new ArgumentException("TenantId is required.");
        if (string.IsNullOrWhiteSpace(filter)) throw new ArgumentException("Filter is required.");

        TenantId = tenantId;
        Bucket = bucket;
        Filter = filter.Trim();

        Key = $"tenantId#{TenantId}:filter#{Filter}:bucket#{Bucket}";
        Hash = ComputeHash($"{TenantId}{Filter}{Bucket}");
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }


    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TenantId;
        yield return Bucket;
        yield return Filter;
    }
}

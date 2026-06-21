using DynamicExpresso;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;

namespace LiveSync.Infrastructure.RealTimeSync;

public sealed class DynamicExpressoFilterEvaluator : IFilterEvaluator
{
    private static readonly Interpreter Interpreter = new();

    public bool Matches(string filter, ICacheDto dto)
    {
        if (dto is not ItemCacheDto item)
            return false;

        try
        {
            var lambda = Interpreter.ParseAsDelegate<Func<ItemCacheDto, bool>>(
                filter,
                "item");

            return lambda(item);
        }
        catch
        {
            return false;
        }
    }

    public bool IsValidFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return false;

        try
        {
            Interpreter.ParseAsDelegate<Func<ItemCacheDto, bool>>(filter, "item");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
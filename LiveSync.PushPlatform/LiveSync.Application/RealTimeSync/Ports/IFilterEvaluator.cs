using LiveSync.Application.RealTimeSync.ReadModels;

namespace LiveSync.Application.RealTimeSync.Ports;

public interface IFilterEvaluator
{
    bool Matches(string filter, ICacheDto dto);
    bool IsValidFilter(string filter);
}

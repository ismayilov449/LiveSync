using DynamicExpresso;
using LiveSync.Application.RealTimeSync.Buckets;
using LiveSync.Application.RealTimeSync.Ports;
using LiveSync.Application.RealTimeSync.ReadModels;
using LiveSync.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LiveSync.Infrastructure.RealTimeSync;

public sealed class DynamicExpressoFilterEvaluator(
    BucketModuleRegistry registry,
    ILogger<DynamicExpressoFilterEvaluator> logger) : IFilterEvaluator
{
    private static readonly Interpreter Interpreter = new();

    public bool Matches(string filter, ICacheDto dto)
    {
        var module = registry.GetRequired(dto.Bucket);

        try
        {
            return Evaluate(filter, dto, module.FilterParameterName, module.DtoClrType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Filter evaluation failed for bucket {Bucket} with filter {Filter}",
                dto.Bucket,
                filter);
            return false;
        }
    }

    public bool IsValidFilter(string filter, TopicBucket bucket)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return false;

        var module = registry.GetRequired(bucket);

        try
        {
            ValidateTyped(filter, module.FilterParameterName, module.DtoClrType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ValidateTyped(string filter, string parameterName, Type dtoType)
    {
        var method = typeof(DynamicExpressoFilterEvaluator)
            .GetMethod(nameof(ValidateTypedGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(dtoType);
        generic.Invoke(null, [filter, parameterName]);
    }

    private static void ValidateTypedGeneric<TDto>(string filter, string parameterName)
        where TDto : class
    {
        Interpreter.ParseAsDelegate<Func<TDto, bool>>(filter, parameterName);
    }

    private static bool Evaluate(string filter, ICacheDto dto, string parameterName, Type dtoType)
    {
        var method = typeof(DynamicExpressoFilterEvaluator)
            .GetMethod(nameof(EvaluateTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(dtoType);
        return (bool)generic.Invoke(null, [filter, dto, parameterName])!;
    }

    private static bool EvaluateTyped<TDto>(string filter, ICacheDto dto, string parameterName)
        where TDto : class
    {
        var lambda = Interpreter.ParseAsDelegate<Func<TDto, bool>>(filter, parameterName);
        return lambda((TDto)dto);
    }
}

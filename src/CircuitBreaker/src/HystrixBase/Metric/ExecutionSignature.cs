// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public class ExecutionSignature
{
    private readonly string _cacheKey;

    private ExecutionSignature(IHystrixCommandKey commandKey, ExecutionResult.EventCounts eventCounts, string cacheKey, int cachedCount, IHystrixCollapserKey collapserKey, int collapserBatchSize)
    {
        CommandName = commandKey.Name;
        Eventcounts = eventCounts;
        _cacheKey = cacheKey;
        CachedCount = cachedCount;
        CollapserKey = collapserKey;
        CollapserBatchSize = collapserBatchSize;
    }

    public static ExecutionSignature From(IHystrixInvokableInfo execution)
    {
        return new ExecutionSignature(execution.CommandKey, execution.EventCounts, null, 0, execution.OriginatingCollapserKey, execution.NumberCollapsed);
    }

    public static ExecutionSignature From(IHystrixInvokableInfo execution, string cacheKey, int cachedCount)
    {
        return new ExecutionSignature(execution.CommandKey, execution.EventCounts, cacheKey, cachedCount, execution.OriginatingCollapserKey, execution.NumberCollapsed);
    }

    public override bool Equals(object o)
    {
        if (this == o)
        {
            return true;
        }

        if (o == null || GetType() != o.GetType())
        {
            return false;
        }

        var that = (ExecutionSignature)o;

        if (!CommandName.Equals(that.CommandName))
        {
            return false;
        }

        if (!Eventcounts.Equals(that.Eventcounts))
        {
            return false;
        }

        return !(!_cacheKey?.Equals(that._cacheKey) ?? that._cacheKey != null);
    }

    public override int GetHashCode()
    {
        var result = CommandName.GetHashCode();
        result = (31 * result) + Eventcounts.GetHashCode();
        result = (31 * result) + (_cacheKey != null ? _cacheKey.GetHashCode() : 0);
        return result;
    }

    public string CommandName { get; }

    public ExecutionResult.EventCounts Eventcounts { get; }

    public int CachedCount { get; }

    public IHystrixCollapserKey CollapserKey { get; }

    public int CollapserBatchSize { get; }
}

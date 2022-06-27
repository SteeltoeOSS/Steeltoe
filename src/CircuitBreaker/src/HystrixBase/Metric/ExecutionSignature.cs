// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

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

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not ExecutionSignature other || GetType() != obj.GetType())
        {
            return false;
        }

        return CommandName == other.CommandName && Equals(Eventcounts, other.Eventcounts) && _cacheKey == other._cacheKey;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CommandName, Eventcounts, _cacheKey);
    }

    public string CommandName { get; }

    public ExecutionResult.EventCounts Eventcounts { get; }

    public int CachedCount { get; }

    public IHystrixCollapserKey CollapserKey { get; }

    public int CollapserBatchSize { get; }
}

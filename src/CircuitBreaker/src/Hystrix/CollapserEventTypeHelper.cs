// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class CollapserEventTypeHelper
{
    public static IList<CollapserEventType> Values { get; } = new List<CollapserEventType>();

    static CollapserEventTypeHelper()
    {
        Values.Add(CollapserEventType.BatchExecuted);
        Values.Add(CollapserEventType.AddedToBatch);
        Values.Add(CollapserEventType.ResponseFromCache);
    }

    public static CollapserEventType From(this HystrixRollingNumberEvent @event)
    {
        return @event switch
        {
            HystrixRollingNumberEvent.CollapserBatch => CollapserEventType.BatchExecuted,
            HystrixRollingNumberEvent.CollapserRequestBatched => CollapserEventType.AddedToBatch,
            HystrixRollingNumberEvent.ResponseFromCache => CollapserEventType.ResponseFromCache,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, $"Value cannot be converted to {nameof(CollapserEventType)}.")
        };
    }
}

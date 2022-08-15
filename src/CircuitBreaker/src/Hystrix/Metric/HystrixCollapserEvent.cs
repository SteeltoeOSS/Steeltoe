// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public class HystrixCollapserEvent : IHystrixEvent
{
    public IHystrixCollapserKey CollapserKey { get; }

    public CollapserEventType EventType { get; }

    public int Count { get; }

    protected HystrixCollapserEvent(IHystrixCollapserKey collapserKey, CollapserEventType eventType, int count)
    {
        CollapserKey = collapserKey;
        EventType = eventType;
        Count = count;
    }

    public static HystrixCollapserEvent From(IHystrixCollapserKey collapserKey, CollapserEventType eventType, int count)
    {
        return new HystrixCollapserEvent(collapserKey, eventType, count);
    }

    public override string ToString()
    {
        return $"HystrixCollapserEvent[{CollapserKey.Name}] : {EventType} : {Count}";
    }
}

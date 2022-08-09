// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class HystrixEventTypeHelper
{
    public static IList<HystrixEventType> Values { get; } = new List<HystrixEventType>
    {
        HystrixEventType.Emit,
        HystrixEventType.Success,
        HystrixEventType.Failure,
        HystrixEventType.Timeout,
        HystrixEventType.BadRequest,
        HystrixEventType.ShortCircuited,
        HystrixEventType.ThreadPoolRejected,
        HystrixEventType.SemaphoreRejected,
        HystrixEventType.FallbackEmit,
        HystrixEventType.FallbackSuccess,
        HystrixEventType.FallbackFailure,
        HystrixEventType.FallbackRejection,
        HystrixEventType.FallbackMissing,
        HystrixEventType.ExceptionThrown,
        HystrixEventType.ResponseFromCache,
        HystrixEventType.Cancelled,
        HystrixEventType.Collapsed
    };

    public static IList<HystrixEventType> ExceptionProducingEventTypes { get; } = new List<HystrixEventType>
    {
        HystrixEventType.BadRequest,
        HystrixEventType.FallbackFailure,
        HystrixEventType.FallbackMissing,
        HystrixEventType.FallbackRejection
    };

    public static IList<HystrixEventType> TerminalEventTypes { get; } = GetTerminalEventTypes();

    public static bool IsTerminal(this HystrixEventType eventType)
    {
        return eventType switch
        {
            HystrixEventType.Emit => false,
            HystrixEventType.Success => false,
            HystrixEventType.Failure => false,
            HystrixEventType.Timeout => false,
            HystrixEventType.BadRequest => false,
            HystrixEventType.ShortCircuited => false,
            HystrixEventType.ThreadPoolRejected => false,
            HystrixEventType.SemaphoreRejected => false,
            HystrixEventType.FallbackEmit => false,
            HystrixEventType.FallbackSuccess => false,
            HystrixEventType.FallbackFailure => false,
            HystrixEventType.FallbackRejection => false,
            HystrixEventType.FallbackMissing => false,
            HystrixEventType.ExceptionThrown => false,
            HystrixEventType.ResponseFromCache => false,
            HystrixEventType.Cancelled => false,
            HystrixEventType.Collapsed => false,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Value cannot be converted.")
        };
    }

    public static HystrixEventType From(this HystrixRollingNumberEvent @event)
    {
        return @event switch
        {
            HystrixRollingNumberEvent.Emit => HystrixEventType.Emit,
            HystrixRollingNumberEvent.Success => HystrixEventType.Success,
            HystrixRollingNumberEvent.Failure => HystrixEventType.Failure,
            HystrixRollingNumberEvent.Timeout => HystrixEventType.Timeout,
            HystrixRollingNumberEvent.ShortCircuited => HystrixEventType.ShortCircuited,
            HystrixRollingNumberEvent.ThreadPoolRejected => HystrixEventType.ThreadPoolRejected,
            HystrixRollingNumberEvent.SemaphoreRejected => HystrixEventType.SemaphoreRejected,
            HystrixRollingNumberEvent.FallbackEmit => HystrixEventType.FallbackEmit,
            HystrixRollingNumberEvent.FallbackSuccess => HystrixEventType.FallbackSuccess,
            HystrixRollingNumberEvent.FallbackFailure => HystrixEventType.FallbackFailure,
            HystrixRollingNumberEvent.FallbackRejection => HystrixEventType.FallbackRejection,
            HystrixRollingNumberEvent.FallbackMissing => HystrixEventType.FallbackMissing,
            HystrixRollingNumberEvent.ExceptionThrown => HystrixEventType.ExceptionThrown,
            HystrixRollingNumberEvent.ResponseFromCache => HystrixEventType.ResponseFromCache,
            HystrixRollingNumberEvent.Collapsed => HystrixEventType.Collapsed,
            HystrixRollingNumberEvent.BadRequest => HystrixEventType.BadRequest,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, $"Value cannot be converted to {nameof(HystrixEventType)}.")
        };
    }

    private static List<HystrixEventType> GetTerminalEventTypes()
    {
        var terminalEventTypes = new List<HystrixEventType>();

        foreach (string evName in Enum.GetNames(typeof(HystrixEventType)))
        {
            var e = (HystrixEventType)Enum.Parse(typeof(HystrixEventType), evName);

            if (e.IsTerminal())
            {
                terminalEventTypes.Add(e);
            }
        }

        return terminalEventTypes;
    }
}

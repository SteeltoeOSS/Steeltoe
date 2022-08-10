// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

internal static class HystrixRollingNumberEventHelper
{
    public static IList<HystrixRollingNumberEvent> Values { get; } = new List<HystrixRollingNumberEvent>();

    static HystrixRollingNumberEventHelper()
    {
        Values.Add(HystrixRollingNumberEvent.Success);
        Values.Add(HystrixRollingNumberEvent.Failure);
        Values.Add(HystrixRollingNumberEvent.Timeout);
        Values.Add(HystrixRollingNumberEvent.ShortCircuited);
        Values.Add(HystrixRollingNumberEvent.ThreadPoolRejected);
        Values.Add(HystrixRollingNumberEvent.SemaphoreRejected);
        Values.Add(HystrixRollingNumberEvent.BadRequest);
        Values.Add(HystrixRollingNumberEvent.FallbackSuccess);
        Values.Add(HystrixRollingNumberEvent.FallbackFailure);
        Values.Add(HystrixRollingNumberEvent.FallbackRejection);
        Values.Add(HystrixRollingNumberEvent.FallbackMissing);
        Values.Add(HystrixRollingNumberEvent.ExceptionThrown);
        Values.Add(HystrixRollingNumberEvent.CommandMaxActive);
        Values.Add(HystrixRollingNumberEvent.Emit);
        Values.Add(HystrixRollingNumberEvent.FallbackEmit);
        Values.Add(HystrixRollingNumberEvent.ThreadExecution);
        Values.Add(HystrixRollingNumberEvent.ThreadMaxActive);
        Values.Add(HystrixRollingNumberEvent.Collapsed);
        Values.Add(HystrixRollingNumberEvent.ResponseFromCache);
        Values.Add(HystrixRollingNumberEvent.CollapserRequestBatched);
        Values.Add(HystrixRollingNumberEvent.CollapserBatch);
    }

    public static HystrixRollingNumberEvent From(HystrixEventType eventType)
    {
        return eventType switch
        {
            HystrixEventType.BadRequest => HystrixRollingNumberEvent.BadRequest,
            HystrixEventType.Collapsed => HystrixRollingNumberEvent.Collapsed,
            HystrixEventType.Emit => HystrixRollingNumberEvent.Emit,
            HystrixEventType.ExceptionThrown => HystrixRollingNumberEvent.ExceptionThrown,
            HystrixEventType.Failure => HystrixRollingNumberEvent.Failure,
            HystrixEventType.FallbackEmit => HystrixRollingNumberEvent.FallbackEmit,
            HystrixEventType.FallbackFailure => HystrixRollingNumberEvent.FallbackFailure,
            HystrixEventType.FallbackMissing => HystrixRollingNumberEvent.FallbackMissing,
            HystrixEventType.FallbackRejection => HystrixRollingNumberEvent.FallbackRejection,
            HystrixEventType.FallbackSuccess => HystrixRollingNumberEvent.FallbackSuccess,
            HystrixEventType.ResponseFromCache => HystrixRollingNumberEvent.ResponseFromCache,
            HystrixEventType.SemaphoreRejected => HystrixRollingNumberEvent.SemaphoreRejected,
            HystrixEventType.ShortCircuited => HystrixRollingNumberEvent.ShortCircuited,
            HystrixEventType.Success => HystrixRollingNumberEvent.Success,
            HystrixEventType.ThreadPoolRejected => HystrixRollingNumberEvent.ThreadPoolRejected,
            HystrixEventType.Timeout => HystrixRollingNumberEvent.Timeout,
            _ => throw new ArgumentOutOfRangeException($"Unknown HystrixEventType : {eventType}")
        };
    }

    public static bool IsCounter(HystrixRollingNumberEvent @event)
    {
        switch (@event)
        {
            case HystrixRollingNumberEvent.Success:
            case HystrixRollingNumberEvent.Failure:
            case HystrixRollingNumberEvent.Timeout:
            case HystrixRollingNumberEvent.ShortCircuited:
            case HystrixRollingNumberEvent.ThreadPoolRejected:
            case HystrixRollingNumberEvent.SemaphoreRejected:
            case HystrixRollingNumberEvent.BadRequest:
            case HystrixRollingNumberEvent.FallbackSuccess:
            case HystrixRollingNumberEvent.FallbackFailure:
            case HystrixRollingNumberEvent.FallbackRejection:
            case HystrixRollingNumberEvent.FallbackMissing:
            case HystrixRollingNumberEvent.ExceptionThrown:
            case HystrixRollingNumberEvent.Emit:
            case HystrixRollingNumberEvent.FallbackEmit:
            case HystrixRollingNumberEvent.ThreadExecution:
            case HystrixRollingNumberEvent.Collapsed:
            case HystrixRollingNumberEvent.ResponseFromCache:
            case HystrixRollingNumberEvent.CollapserRequestBatched:
            case HystrixRollingNumberEvent.CollapserBatch:
                return true;
            default:
                return false;
        }
    }

    public static bool IsMaxUpdater(HystrixRollingNumberEvent @event)
    {
        switch (@event)
        {
            case HystrixRollingNumberEvent.CommandMaxActive:
            case HystrixRollingNumberEvent.ThreadMaxActive:
                return true;
            default:
                return false;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    internal static class HystrixRollingNumberEventHelper
    {
        static HystrixRollingNumberEventHelper()
        {
            Values.Add(HystrixRollingNumberEvent.SUCCESS);
            Values.Add(HystrixRollingNumberEvent.FAILURE);
            Values.Add(HystrixRollingNumberEvent.TIMEOUT);
            Values.Add(HystrixRollingNumberEvent.SHORT_CIRCUITED);
            Values.Add(HystrixRollingNumberEvent.THREAD_POOL_REJECTED);
            Values.Add(HystrixRollingNumberEvent.SEMAPHORE_REJECTED);
            Values.Add(HystrixRollingNumberEvent.BAD_REQUEST);
            Values.Add(HystrixRollingNumberEvent.FALLBACK_SUCCESS);
            Values.Add(HystrixRollingNumberEvent.FALLBACK_FAILURE);
            Values.Add(HystrixRollingNumberEvent.FALLBACK_REJECTION);
            Values.Add(HystrixRollingNumberEvent.FALLBACK_MISSING);
            Values.Add(HystrixRollingNumberEvent.EXCEPTION_THROWN);
            Values.Add(HystrixRollingNumberEvent.COMMAND_MAX_ACTIVE);
            Values.Add(HystrixRollingNumberEvent.EMIT);
            Values.Add(HystrixRollingNumberEvent.FALLBACK_EMIT);
            Values.Add(HystrixRollingNumberEvent.THREAD_EXECUTION);
            Values.Add(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE);
            Values.Add(HystrixRollingNumberEvent.COLLAPSED);
            Values.Add(HystrixRollingNumberEvent.RESPONSE_FROM_CACHE);
            Values.Add(HystrixRollingNumberEvent.COLLAPSER_REQUEST_BATCHED);
            Values.Add(HystrixRollingNumberEvent.COLLAPSER_BATCH);
        }

        public static IList<HystrixRollingNumberEvent> Values { get; } = new List<HystrixRollingNumberEvent>();

        public static HystrixRollingNumberEvent From(HystrixEventType eventType)
        {
            return eventType switch
            {
                HystrixEventType.BAD_REQUEST => HystrixRollingNumberEvent.BAD_REQUEST,
                HystrixEventType.COLLAPSED => HystrixRollingNumberEvent.COLLAPSED,
                HystrixEventType.EMIT => HystrixRollingNumberEvent.EMIT,
                HystrixEventType.EXCEPTION_THROWN => HystrixRollingNumberEvent.EXCEPTION_THROWN,
                HystrixEventType.FAILURE => HystrixRollingNumberEvent.FAILURE,
                HystrixEventType.FALLBACK_EMIT => HystrixRollingNumberEvent.FALLBACK_EMIT,
                HystrixEventType.FALLBACK_FAILURE => HystrixRollingNumberEvent.FALLBACK_FAILURE,
                HystrixEventType.FALLBACK_MISSING => HystrixRollingNumberEvent.FALLBACK_MISSING,
                HystrixEventType.FALLBACK_REJECTION => HystrixRollingNumberEvent.FALLBACK_REJECTION,
                HystrixEventType.FALLBACK_SUCCESS => HystrixRollingNumberEvent.FALLBACK_SUCCESS,
                HystrixEventType.RESPONSE_FROM_CACHE => HystrixRollingNumberEvent.RESPONSE_FROM_CACHE,
                HystrixEventType.SEMAPHORE_REJECTED => HystrixRollingNumberEvent.SEMAPHORE_REJECTED,
                HystrixEventType.SHORT_CIRCUITED => HystrixRollingNumberEvent.SHORT_CIRCUITED,
                HystrixEventType.SUCCESS => HystrixRollingNumberEvent.SUCCESS,
                HystrixEventType.THREAD_POOL_REJECTED => HystrixRollingNumberEvent.THREAD_POOL_REJECTED,
                HystrixEventType.TIMEOUT => HystrixRollingNumberEvent.TIMEOUT,
                _ => throw new ArgumentOutOfRangeException($"Unknown HystrixEventType : {eventType}"),
            };
        }

        public static bool IsCounter(HystrixRollingNumberEvent @event)
        {
            switch (@event)
            {
                case HystrixRollingNumberEvent.SUCCESS:
                case HystrixRollingNumberEvent.FAILURE:
                case HystrixRollingNumberEvent.TIMEOUT:
                case HystrixRollingNumberEvent.SHORT_CIRCUITED:
                case HystrixRollingNumberEvent.THREAD_POOL_REJECTED:
                case HystrixRollingNumberEvent.SEMAPHORE_REJECTED:
                case HystrixRollingNumberEvent.BAD_REQUEST:
                case HystrixRollingNumberEvent.FALLBACK_SUCCESS:
                case HystrixRollingNumberEvent.FALLBACK_FAILURE:
                case HystrixRollingNumberEvent.FALLBACK_REJECTION:
                case HystrixRollingNumberEvent.FALLBACK_MISSING:
                case HystrixRollingNumberEvent.EXCEPTION_THROWN:
                case HystrixRollingNumberEvent.EMIT:
                case HystrixRollingNumberEvent.FALLBACK_EMIT:
                case HystrixRollingNumberEvent.THREAD_EXECUTION:
                case HystrixRollingNumberEvent.COLLAPSED:
                case HystrixRollingNumberEvent.RESPONSE_FROM_CACHE:
                case HystrixRollingNumberEvent.COLLAPSER_REQUEST_BATCHED:
                case HystrixRollingNumberEvent.COLLAPSER_BATCH:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsMaxUpdater(HystrixRollingNumberEvent @event)
        {
            switch (@event)
            {
                case HystrixRollingNumberEvent.COMMAND_MAX_ACTIVE:
                case HystrixRollingNumberEvent.THREAD_MAX_ACTIVE:
                    return true;
                default:
                    return false;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixEventTypeHelper
    {
        static HystrixEventTypeHelper()
        {
            ExceptionProducingEventTypes.Add(HystrixEventType.BAD_REQUEST);
            ExceptionProducingEventTypes.Add(HystrixEventType.FALLBACK_FAILURE);
            ExceptionProducingEventTypes.Add(HystrixEventType.FALLBACK_MISSING);
            ExceptionProducingEventTypes.Add(HystrixEventType.FALLBACK_REJECTION);

            foreach (var evName in Enum.GetNames(typeof(HystrixEventType)))
            {
                var e = (HystrixEventType)Enum.Parse(typeof(HystrixEventType), evName);
                if (e.IsTerminal())
                {
                    TerminalEventTypes.Add(e);
                }
            }

            Values.Add(HystrixEventType.EMIT);
            Values.Add(HystrixEventType.SUCCESS);
            Values.Add(HystrixEventType.FAILURE);
            Values.Add(HystrixEventType.TIMEOUT);
            Values.Add(HystrixEventType.BAD_REQUEST);
            Values.Add(HystrixEventType.SHORT_CIRCUITED);
            Values.Add(HystrixEventType.THREAD_POOL_REJECTED);
            Values.Add(HystrixEventType.SEMAPHORE_REJECTED);
            Values.Add(HystrixEventType.FALLBACK_EMIT);
            Values.Add(HystrixEventType.FALLBACK_SUCCESS);
            Values.Add(HystrixEventType.FALLBACK_FAILURE);
            Values.Add(HystrixEventType.FALLBACK_REJECTION);
            Values.Add(HystrixEventType.FALLBACK_MISSING);
            Values.Add(HystrixEventType.EXCEPTION_THROWN);
            Values.Add(HystrixEventType.RESPONSE_FROM_CACHE);
            Values.Add(HystrixEventType.CANCELLED);
            Values.Add(HystrixEventType.COLLAPSED);
        }

        public static bool IsTerminal(this HystrixEventType evType)
        {
            return evType switch
            {
                HystrixEventType.EMIT => false,
                HystrixEventType.SUCCESS => false,
                HystrixEventType.FAILURE => false,
                HystrixEventType.TIMEOUT => false,
                HystrixEventType.BAD_REQUEST => false,
                HystrixEventType.SHORT_CIRCUITED => false,
                HystrixEventType.THREAD_POOL_REJECTED => false,
                HystrixEventType.SEMAPHORE_REJECTED => false,
                HystrixEventType.FALLBACK_EMIT => false,
                HystrixEventType.FALLBACK_SUCCESS => false,
                HystrixEventType.FALLBACK_FAILURE => false,
                HystrixEventType.FALLBACK_REJECTION => false,
                HystrixEventType.FALLBACK_MISSING => false,
                HystrixEventType.EXCEPTION_THROWN => false,
                HystrixEventType.RESPONSE_FROM_CACHE => false,
                HystrixEventType.CANCELLED => false,
                HystrixEventType.COLLAPSED => false,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static HystrixEventType From(this HystrixRollingNumberEvent @event)
        {
            return @event switch
            {
                HystrixRollingNumberEvent.EMIT => HystrixEventType.EMIT,
                HystrixRollingNumberEvent.SUCCESS => HystrixEventType.SUCCESS,
                HystrixRollingNumberEvent.FAILURE => HystrixEventType.FAILURE,
                HystrixRollingNumberEvent.TIMEOUT => HystrixEventType.TIMEOUT,
                HystrixRollingNumberEvent.SHORT_CIRCUITED => HystrixEventType.SHORT_CIRCUITED,
                HystrixRollingNumberEvent.THREAD_POOL_REJECTED => HystrixEventType.THREAD_POOL_REJECTED,
                HystrixRollingNumberEvent.SEMAPHORE_REJECTED => HystrixEventType.SEMAPHORE_REJECTED,
                HystrixRollingNumberEvent.FALLBACK_EMIT => HystrixEventType.FALLBACK_EMIT,
                HystrixRollingNumberEvent.FALLBACK_SUCCESS => HystrixEventType.FALLBACK_SUCCESS,
                HystrixRollingNumberEvent.FALLBACK_FAILURE => HystrixEventType.FALLBACK_FAILURE,
                HystrixRollingNumberEvent.FALLBACK_REJECTION => HystrixEventType.FALLBACK_REJECTION,
                HystrixRollingNumberEvent.FALLBACK_MISSING => HystrixEventType.FALLBACK_MISSING,
                HystrixRollingNumberEvent.EXCEPTION_THROWN => HystrixEventType.EXCEPTION_THROWN,
                HystrixRollingNumberEvent.RESPONSE_FROM_CACHE => HystrixEventType.RESPONSE_FROM_CACHE,
                HystrixRollingNumberEvent.COLLAPSED => HystrixEventType.COLLAPSED,
                HystrixRollingNumberEvent.BAD_REQUEST => HystrixEventType.BAD_REQUEST,
                _ => throw new ArgumentOutOfRangeException($"Not an event that can be converted to HystrixEventType : {@event}"),
            };
        }

        public static IList<HystrixEventType> Values { get; } = new List<HystrixEventType>();

        public static IList<HystrixEventType> ExceptionProducingEventTypes { get; } = new List<HystrixEventType>();

        public static IList<HystrixEventType> TerminalEventTypes { get; } = new List<HystrixEventType>();
    }
}

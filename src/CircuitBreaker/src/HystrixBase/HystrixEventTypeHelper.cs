// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            switch (evType)
            {
                case HystrixEventType.EMIT:
                    return false;
                case HystrixEventType.SUCCESS:
                    return false;
                case HystrixEventType.FAILURE:
                    return false;
                case HystrixEventType.TIMEOUT:
                    return false;
                case HystrixEventType.BAD_REQUEST:
                    return false;
                case HystrixEventType.SHORT_CIRCUITED:
                    return false;
                case HystrixEventType.THREAD_POOL_REJECTED:
                    return false;
                case HystrixEventType.SEMAPHORE_REJECTED:
                    return false;
                case HystrixEventType.FALLBACK_EMIT:
                    return false;
                case HystrixEventType.FALLBACK_SUCCESS:
                    return false;
                case HystrixEventType.FALLBACK_FAILURE:
                    return false;
                case HystrixEventType.FALLBACK_REJECTION:
                    return false;
                case HystrixEventType.FALLBACK_MISSING:
                    return false;
                case HystrixEventType.EXCEPTION_THROWN:
                    return false;
                case HystrixEventType.RESPONSE_FROM_CACHE:
                    return false;
                case HystrixEventType.CANCELLED:
                    return false;
                case HystrixEventType.COLLAPSED:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static HystrixEventType From(this HystrixRollingNumberEvent @event)
        {
            switch (@event)
            {
                case HystrixRollingNumberEvent.EMIT:
                    return HystrixEventType.EMIT;
                case HystrixRollingNumberEvent.SUCCESS:
                    return HystrixEventType.SUCCESS;
                case HystrixRollingNumberEvent.FAILURE:
                    return HystrixEventType.FAILURE;
                case HystrixRollingNumberEvent.TIMEOUT:
                    return HystrixEventType.TIMEOUT;
                case HystrixRollingNumberEvent.SHORT_CIRCUITED:
                    return HystrixEventType.SHORT_CIRCUITED;
                case HystrixRollingNumberEvent.THREAD_POOL_REJECTED:
                    return HystrixEventType.THREAD_POOL_REJECTED;
                case HystrixRollingNumberEvent.SEMAPHORE_REJECTED:
                    return HystrixEventType.SEMAPHORE_REJECTED;
                case HystrixRollingNumberEvent.FALLBACK_EMIT:
                    return HystrixEventType.FALLBACK_EMIT;
                case HystrixRollingNumberEvent.FALLBACK_SUCCESS:
                    return HystrixEventType.FALLBACK_SUCCESS;
                case HystrixRollingNumberEvent.FALLBACK_FAILURE:
                    return HystrixEventType.FALLBACK_FAILURE;
                case HystrixRollingNumberEvent.FALLBACK_REJECTION:
                    return HystrixEventType.FALLBACK_REJECTION;
                case HystrixRollingNumberEvent.FALLBACK_MISSING:
                    return HystrixEventType.FALLBACK_MISSING;
                case HystrixRollingNumberEvent.EXCEPTION_THROWN:
                    return HystrixEventType.EXCEPTION_THROWN;
                case HystrixRollingNumberEvent.RESPONSE_FROM_CACHE:
                    return HystrixEventType.RESPONSE_FROM_CACHE;
                case HystrixRollingNumberEvent.COLLAPSED:
                    return HystrixEventType.COLLAPSED;
                case HystrixRollingNumberEvent.BAD_REQUEST:
                    return HystrixEventType.BAD_REQUEST;
                default:
                    throw new ArgumentOutOfRangeException("Not an event that can be converted to HystrixEventType : " + @event);
            }
        }

        public static IList<HystrixEventType> Values { get; } = new List<HystrixEventType>();

        public static IList<HystrixEventType> ExceptionProducingEventTypes { get; } = new List<HystrixEventType>();

        public static IList<HystrixEventType> TerminalEventTypes { get; } = new List<HystrixEventType>();
    }
}

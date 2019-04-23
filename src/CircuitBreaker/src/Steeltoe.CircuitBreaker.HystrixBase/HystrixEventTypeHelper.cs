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
        public static readonly IList<HystrixEventType> EXCEPTION_PRODUCING_EVENT_TYPES = new List<HystrixEventType>();

        public static readonly IList<HystrixEventType> TERMINAL_EVENT_TYPES = new List<HystrixEventType>();

        private static readonly IList<HystrixEventType> ValueList = new List<HystrixEventType>();

        static HystrixEventTypeHelper()
        {
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.BAD_REQUEST);
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.FALLBACK_FAILURE);
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.FALLBACK_MISSING);
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.FALLBACK_REJECTION);

            foreach (string evName in Enum.GetNames(typeof(HystrixEventType)))
            {
                HystrixEventType e = (HystrixEventType)Enum.Parse(typeof(HystrixEventType), evName);
                if (e.IsTerminal())
                {
                    TERMINAL_EVENT_TYPES.Add(e);
                }
            }

            ValueList.Add(HystrixEventType.EMIT);
            ValueList.Add(HystrixEventType.SUCCESS);
            ValueList.Add(HystrixEventType.FAILURE);
            ValueList.Add(HystrixEventType.TIMEOUT);
            ValueList.Add(HystrixEventType.BAD_REQUEST);
            ValueList.Add(HystrixEventType.SHORT_CIRCUITED);
            ValueList.Add(HystrixEventType.THREAD_POOL_REJECTED);
            ValueList.Add(HystrixEventType.SEMAPHORE_REJECTED);
            ValueList.Add(HystrixEventType.FALLBACK_EMIT);
            ValueList.Add(HystrixEventType.FALLBACK_SUCCESS);
            ValueList.Add(HystrixEventType.FALLBACK_FAILURE);
            ValueList.Add(HystrixEventType.FALLBACK_REJECTION);
            ValueList.Add(HystrixEventType.FALLBACK_MISSING);
            ValueList.Add(HystrixEventType.EXCEPTION_THROWN);
            ValueList.Add(HystrixEventType.RESPONSE_FROM_CACHE);
            ValueList.Add(HystrixEventType.CANCELLED);
            ValueList.Add(HystrixEventType.COLLAPSED);
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

        public static IList<HystrixEventType> Values
        {
            get { return ValueList; }
        }
    }
}

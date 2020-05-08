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
    public static class ThreadPoolEventTypeHelper
    {
        public static IList<ThreadPoolEventType> Values { get; } = new List<ThreadPoolEventType>();

        static ThreadPoolEventTypeHelper()
        {
            Values.Add(ThreadPoolEventType.EXECUTED);
            Values.Add(ThreadPoolEventType.REJECTED);
        }

        public static ThreadPoolEventType From(this HystrixRollingNumberEvent @event)
        {
            switch (@event)
            {
                case HystrixRollingNumberEvent.THREAD_EXECUTION:
                    return ThreadPoolEventType.EXECUTED;
                case HystrixRollingNumberEvent.THREAD_POOL_REJECTED:
                    return ThreadPoolEventType.REJECTED;
                default:
                    throw new ArgumentOutOfRangeException("Not an event that can be converted to HystrixEventType.ThreadPool : " + @event);
            }
        }

        public static ThreadPoolEventType From(this HystrixEventType eventType)
        {
            switch (eventType)
            {
                case HystrixEventType.SUCCESS:
                    return ThreadPoolEventType.EXECUTED;
                case HystrixEventType.FAILURE:
                    return ThreadPoolEventType.EXECUTED;
                case HystrixEventType.TIMEOUT:
                    return ThreadPoolEventType.EXECUTED;
                case HystrixEventType.BAD_REQUEST:
                    return ThreadPoolEventType.EXECUTED;
                case HystrixEventType.THREAD_POOL_REJECTED:
                    return ThreadPoolEventType.REJECTED;
                default:
                    return ThreadPoolEventType.UNKNOWN;
            }
        }
    }
}

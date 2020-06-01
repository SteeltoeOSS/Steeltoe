// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class ThreadPoolEventTypeHelper
    {
        private static readonly IList<ThreadPoolEventType> ValueList = new List<ThreadPoolEventType>();

        public static IList<ThreadPoolEventType> Values
        {
            get { return ValueList; }
        }

        static ThreadPoolEventTypeHelper()
        {
            ValueList.Add(ThreadPoolEventType.EXECUTED);
            ValueList.Add(ThreadPoolEventType.REJECTED);
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

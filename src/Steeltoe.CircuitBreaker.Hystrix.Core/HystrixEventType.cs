//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
    /// <summary>
    /// Various states/events that execution can result in or have tracked.
    /// <para>
    /// These are most often accessed via <seealso cref="HystrixRequestLog"/> or <seealso cref="HystrixCommand#getExecutionEvents()"/>.
    /// </para>
    /// </summary>
    public enum HystrixEventType
    {
        EMIT,
        SUCCESS,
        FAILURE,
        TIMEOUT,
        BAD_REQUEST,
        SHORT_CIRCUITED,
        THREAD_POOL_REJECTED,
        SEMAPHORE_REJECTED,
        FALLBACK_EMIT,
        FALLBACK_SUCCESS,
        FALLBACK_FAILURE,
        FALLBACK_REJECTION,
        FALLBACK_MISSING,
        EXCEPTION_THROWN,
        RESPONSE_FROM_CACHE,
        CANCELLED,
        COLLAPSED
    };

    public static class HystrixEventTypeHelper
    {

        private static readonly IList<HystrixEventType> valueList = new List<HystrixEventType>();

        /// <summary>
        /// List of events that throw an Exception to the caller
        /// </summary>
        public static readonly IList<HystrixEventType> EXCEPTION_PRODUCING_EVENT_TYPES = new List<HystrixEventType>();

        /// <summary>
        /// List of events that are terminal
        /// </summary>
        public static readonly IList<HystrixEventType> TERMINAL_EVENT_TYPES = new List<HystrixEventType>();

        static HystrixEventTypeHelper()
        {
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.BAD_REQUEST);
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.FALLBACK_FAILURE);
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.FALLBACK_MISSING);
            EXCEPTION_PRODUCING_EVENT_TYPES.Add(HystrixEventType.FALLBACK_REJECTION);
     

            foreach (string evName in Enum.GetNames(typeof(HystrixEventType)))
            {
                HystrixEventType e = (HystrixEventType) Enum.Parse(typeof(HystrixEventType), evName);
                if (e.IsTerminal())
                {
                    TERMINAL_EVENT_TYPES.Add(e);
                }
            }

            valueList.Add(HystrixEventType.EMIT);
            valueList.Add(HystrixEventType.SUCCESS);
            valueList.Add(HystrixEventType.FAILURE);
            valueList.Add(HystrixEventType.TIMEOUT);
            valueList.Add(HystrixEventType.BAD_REQUEST);
            valueList.Add(HystrixEventType.SHORT_CIRCUITED);
            valueList.Add(HystrixEventType.THREAD_POOL_REJECTED);
            valueList.Add(HystrixEventType.SEMAPHORE_REJECTED);
            valueList.Add(HystrixEventType.FALLBACK_EMIT);
            valueList.Add(HystrixEventType.FALLBACK_SUCCESS);
            valueList.Add(HystrixEventType.FALLBACK_FAILURE);
            valueList.Add(HystrixEventType.FALLBACK_REJECTION);
            valueList.Add(HystrixEventType.FALLBACK_MISSING);
            valueList.Add(HystrixEventType.EXCEPTION_THROWN);
            valueList.Add(HystrixEventType.RESPONSE_FROM_CACHE);
            valueList.Add(HystrixEventType.CANCELLED);
            valueList.Add(HystrixEventType.COLLAPSED);
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
            get { return valueList; }
        }
    }


    public enum ThreadPoolEventType
    {
        EXECUTED,
        REJECTED,
        UNKNOWN
    };

    public static class ThreadPoolEventTypeHelper
    {
        private static readonly IList<ThreadPoolEventType> valueList = new List<ThreadPoolEventType>();

        public static IList<ThreadPoolEventType> Values
        {
            get { return valueList; }
        }

        static ThreadPoolEventTypeHelper()
        {

            valueList.Add(ThreadPoolEventType.EXECUTED);
            valueList.Add(ThreadPoolEventType.REJECTED);
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


    public enum CollapserEventType
    {
        BATCH_EXECUTED,
        ADDED_TO_BATCH,
        RESPONSE_FROM_CACHE
    };

    public static class CollapserEventTypeHelper {

        private static readonly IList<CollapserEventType> valueList = new List<CollapserEventType>();

        public static IList<CollapserEventType> Values
        {
            get { return valueList; }
        }

        static CollapserEventTypeHelper()
        {

            valueList.Add(CollapserEventType.BATCH_EXECUTED);
            valueList.Add(CollapserEventType.ADDED_TO_BATCH);
            valueList.Add(CollapserEventType.RESPONSE_FROM_CACHE);
        }
        public static CollapserEventType From(this HystrixRollingNumberEvent @event)
        {
            switch (@event)
            {
                case HystrixRollingNumberEvent.COLLAPSER_BATCH:
                    return CollapserEventType.BATCH_EXECUTED;
                case HystrixRollingNumberEvent.COLLAPSER_REQUEST_BATCHED:
                    return CollapserEventType.ADDED_TO_BATCH;
                case HystrixRollingNumberEvent.RESPONSE_FROM_CACHE:
                    return CollapserEventType.RESPONSE_FROM_CACHE;
                default:
                    throw new ArgumentOutOfRangeException("Not an event that can be converted to HystrixEventType.Collapser : " + @event);
            }
        }
    }
}
   

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

using System.Collections.Generic;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public enum HystrixRollingNumberEvent
    {
        SUCCESS,
        FAILURE,
        TIMEOUT,
        SHORT_CIRCUITED,
        THREAD_POOL_REJECTED,
        SEMAPHORE_REJECTED,
        BAD_REQUEST,
        FALLBACK_SUCCESS,
        FALLBACK_FAILURE,
        FALLBACK_REJECTION,
        FALLBACK_MISSING,
        EXCEPTION_THROWN,
        COMMAND_MAX_ACTIVE,
        EMIT,
        FALLBACK_EMIT,
        THREAD_EXECUTION,
        THREAD_MAX_ACTIVE,
        COLLAPSED,
        RESPONSE_FROM_CACHE,
        COLLAPSER_REQUEST_BATCHED,
        COLLAPSER_BATCH
    }
    public static class HystrixRollingNumberEventHelper
    {
        private static readonly IList<HystrixRollingNumberEvent> valueList = new List<HystrixRollingNumberEvent>();

        static HystrixRollingNumberEventHelper()
        {
            valueList.Add(HystrixRollingNumberEvent.SUCCESS);
            valueList.Add(HystrixRollingNumberEvent.FAILURE);
            valueList.Add(HystrixRollingNumberEvent.TIMEOUT);
            valueList.Add(HystrixRollingNumberEvent.SHORT_CIRCUITED);
            valueList.Add(HystrixRollingNumberEvent.THREAD_POOL_REJECTED);
            valueList.Add(HystrixRollingNumberEvent.SEMAPHORE_REJECTED);
            valueList.Add(HystrixRollingNumberEvent.BAD_REQUEST);
            valueList.Add(HystrixRollingNumberEvent.FALLBACK_SUCCESS);
            valueList.Add(HystrixRollingNumberEvent.FALLBACK_FAILURE);
            valueList.Add(HystrixRollingNumberEvent.FALLBACK_REJECTION);
            valueList.Add(HystrixRollingNumberEvent.FALLBACK_MISSING);
            valueList.Add(HystrixRollingNumberEvent.EXCEPTION_THROWN);
            valueList.Add(HystrixRollingNumberEvent.COMMAND_MAX_ACTIVE);
            valueList.Add(HystrixRollingNumberEvent.EMIT);
            valueList.Add(HystrixRollingNumberEvent.FALLBACK_EMIT);
            valueList.Add(HystrixRollingNumberEvent.THREAD_EXECUTION);
            valueList.Add(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE);
            valueList.Add(HystrixRollingNumberEvent.COLLAPSED);
            valueList.Add(HystrixRollingNumberEvent.RESPONSE_FROM_CACHE);
            valueList.Add(HystrixRollingNumberEvent.COLLAPSER_REQUEST_BATCHED);
            valueList.Add(HystrixRollingNumberEvent.COLLAPSER_BATCH);
        }
        public static IList<HystrixRollingNumberEvent> Values
        {
            get { return valueList; }
        }
        public static HystrixRollingNumberEvent From(HystrixEventType eventType)
        {
            switch (eventType)
            {
                case HystrixEventType.BAD_REQUEST:
                    return HystrixRollingNumberEvent.BAD_REQUEST;
                case HystrixEventType.COLLAPSED:
                    return HystrixRollingNumberEvent.COLLAPSED;
                case HystrixEventType.EMIT:
                    return HystrixRollingNumberEvent.EMIT;
                case HystrixEventType.EXCEPTION_THROWN:
                    return HystrixRollingNumberEvent.EXCEPTION_THROWN;
                case HystrixEventType.FAILURE:
                    return HystrixRollingNumberEvent.FAILURE;
                case HystrixEventType.FALLBACK_EMIT:
                    return HystrixRollingNumberEvent.FALLBACK_EMIT;
                case HystrixEventType.FALLBACK_FAILURE:
                    return HystrixRollingNumberEvent.FALLBACK_FAILURE;
                case HystrixEventType.FALLBACK_MISSING:
                    return HystrixRollingNumberEvent.FALLBACK_MISSING;
                case HystrixEventType.FALLBACK_REJECTION:
                    return HystrixRollingNumberEvent.FALLBACK_REJECTION;
                case HystrixEventType.FALLBACK_SUCCESS:
                    return HystrixRollingNumberEvent.FALLBACK_SUCCESS;
                case HystrixEventType.RESPONSE_FROM_CACHE:
                    return HystrixRollingNumberEvent.RESPONSE_FROM_CACHE;
                case HystrixEventType.SEMAPHORE_REJECTED:
                    return HystrixRollingNumberEvent.SEMAPHORE_REJECTED;
                case HystrixEventType.SHORT_CIRCUITED:
                    return HystrixRollingNumberEvent.SHORT_CIRCUITED;
                case HystrixEventType.SUCCESS:
                    return HystrixRollingNumberEvent.SUCCESS;
                case HystrixEventType.THREAD_POOL_REJECTED:
                    return HystrixRollingNumberEvent.THREAD_POOL_REJECTED;
                case HystrixEventType.TIMEOUT:
                    return HystrixRollingNumberEvent.TIMEOUT;
                default:
                    throw new ArgumentOutOfRangeException("Unknown HystrixEventType : " + eventType);
            }
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
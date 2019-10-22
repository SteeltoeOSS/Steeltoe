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
    public static class CollapserEventTypeHelper
    {
        public static IList<CollapserEventType> Values { get; } = new List<CollapserEventType>();

        static CollapserEventTypeHelper()
        {
            Values.Add(CollapserEventType.BATCH_EXECUTED);
            Values.Add(CollapserEventType.ADDED_TO_BATCH);
            Values.Add(CollapserEventType.RESPONSE_FROM_CACHE);
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

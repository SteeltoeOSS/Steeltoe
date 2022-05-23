// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            return @event switch
            {
                HystrixRollingNumberEvent.COLLAPSER_BATCH => CollapserEventType.BATCH_EXECUTED,
                HystrixRollingNumberEvent.COLLAPSER_REQUEST_BATCHED => CollapserEventType.ADDED_TO_BATCH,
                HystrixRollingNumberEvent.RESPONSE_FROM_CACHE => CollapserEventType.RESPONSE_FROM_CACHE,
                _ => throw new ArgumentOutOfRangeException($"Not an event that can be converted to HystrixEventType.Collapser : {@event}"),
            };
        }
    }
}

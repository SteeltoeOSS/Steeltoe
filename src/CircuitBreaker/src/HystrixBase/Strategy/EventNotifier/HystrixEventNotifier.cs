// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier
{
    public abstract class HystrixEventNotifier
    {
        public void MarkEvent(HystrixEventType eventType, IHystrixCommandKey key)
        {
            // do nothing
        }

        public void MarkCommandExecution(IHystrixCommandKey key, ExecutionIsolationStrategy isolationStrategy, int duration, IList<HystrixEventType> eventsDuringExecution)
        {
            // do nothing
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser
{
    internal class CollapsedTask<BatchReturnType, ResponseType, RequestArgumentType> : ITimerListener
    {
        private readonly RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> rq;

        public CollapsedTask(RequestCollapser<BatchReturnType, ResponseType, RequestArgumentType> rq)
        {
            this.rq = rq;
        }

        public void Tick()
        {
            try
            {
                // we fetch current so that when multiple threads race
                // we can do compareAndSet with the expected/new to ensure only one happens
                RequestBatch<BatchReturnType, ResponseType, RequestArgumentType> currentBatch = rq.Batch.Value;

                // 1) it can be null if it got shutdown
                // 2) we don't execute this batch if it has no requests and let it wait until next tick to be executed
                if (currentBatch != null && currentBatch.Size > 0)
                {
                    // do execution within context of wrapped Callable
                    rq.CreateNewBatchAndExecutePreviousIfNeeded(currentBatch);
                }
            }
            catch (Exception)
            {
                // logger.error("Error occurred trying to execute callable inside CollapsedTask from Timer.", e);
            }
        }

        public int IntervalTimeInMilliseconds => rq.Properties.TimerDelayInMilliseconds;
    }
}

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

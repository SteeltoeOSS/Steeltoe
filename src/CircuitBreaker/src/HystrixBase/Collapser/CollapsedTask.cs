// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser;

internal sealed class CollapsedTask<TBatchReturn, TResponse, TRequestArgument> : ITimerListener
{
    private readonly RequestCollapser<TBatchReturn, TResponse, TRequestArgument> _rq;

    public int IntervalTimeInMilliseconds => _rq.Properties.TimerDelayInMilliseconds;

    public CollapsedTask(RequestCollapser<TBatchReturn, TResponse, TRequestArgument> rq)
    {
        _rq = rq;
    }

    public void Tick()
    {
        try
        {
            // we fetch current so that when multiple threads race
            // we can do compareAndSet with the expected/new to ensure only one happens
            RequestBatch<TBatchReturn, TResponse, TRequestArgument> currentBatch = _rq.Batch.Value;

            // 1) it can be null if it got shutdown
            // 2) we don't execute this batch if it has no requests and let it wait until next tick to be executed
            if (currentBatch != null && currentBatch.Size > 0)
            {
                // do execution within context of wrapped Callable
                _rq.CreateNewBatchAndExecutePreviousIfNeeded(currentBatch);
            }
        }
        catch (Exception)
        {
            // logger.error("Error occurred trying to execute callable inside CollapsedTask from Timer.", e);
        }
    }
}

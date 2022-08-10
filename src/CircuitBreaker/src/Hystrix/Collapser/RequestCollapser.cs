// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser;

public class RequestCollapser<TBatchReturn, TRequestResponse, TRequestArgument>
{
    private readonly HystrixCollapser<TBatchReturn, TRequestResponse, TRequestArgument> _commandCollapser;
    private readonly AtomicReference<TimerReference> _timerListenerReference = new();
    private readonly AtomicBoolean _timerListenerRegistered = new();
    private readonly ICollapserTimer _timer;
    private readonly HystrixConcurrencyStrategy _concurrencyStrategy;

    public AtomicReference<RequestBatch<TBatchReturn, TRequestResponse, TRequestArgument>> Batch { get; } = new();

    public IHystrixCollapserOptions Properties { get; }

    internal RequestCollapser(HystrixCollapser<TBatchReturn, TRequestResponse, TRequestArgument> commandCollapser, IHystrixCollapserOptions properties,
        ICollapserTimer timer, HystrixConcurrencyStrategy concurrencyStrategy)
    {
        // the command with implementation of abstract methods we need
        _commandCollapser = commandCollapser;
        _concurrencyStrategy = concurrencyStrategy;
        Properties = properties;
        _timer = timer;
        Batch.Value = new RequestBatch<TBatchReturn, TRequestResponse, TRequestArgument>(properties, commandCollapser, properties.MaxRequestsInBatch);
    }

    public CollapsedRequest<TRequestResponse, TRequestArgument> SubmitRequest(TRequestArgument arg, CancellationToken token)
    {
        /*
         * We only want the timer ticking if there are actually things to do so we register it the first time something is added.
         */
        if (!_timerListenerRegistered.Value && _timerListenerRegistered.CompareAndSet(false, true))
        {
            /* schedule the collapsing task to be executed every x milliseconds (x defined inside CollapsedTask) */
            _timerListenerReference.Value = _timer.AddListener(new CollapsedTask<TBatchReturn, TRequestResponse, TRequestArgument>(this));
        }

        // loop until succeed (compare-and-set spin-loop)
        while (true)
        {
            RequestBatch<TBatchReturn, TRequestResponse, TRequestArgument> b = Batch.Value;

            if (b == null)
            {
                throw new InvalidOperationException("Submitting requests after collapser is shutdown");
            }

            CollapsedRequest<TRequestResponse, TRequestArgument> response = b.Offer(arg, token);

            // it will always get an CollapsedRequest unless we hit the max batch size
            if (response != null)
            {
                return response;
            }

            // this batch can't accept requests so create a new one and set it if another thread doesn't beat us
            CreateNewBatchAndExecutePreviousIfNeeded(b);
        }
    }

    public void Shutdown()
    {
        RequestBatch<TBatchReturn, TRequestResponse, TRequestArgument> currentBatch = Batch.GetAndSet(null);

        if (currentBatch != null)
        {
            currentBatch.Shutdown();
        }

        if (_timerListenerReference.Value != null)
        {
            // if the timer was started we'll clear it so it stops ticking
            _timerListenerReference.Value.Dispose();
        }
    }

    internal void CreateNewBatchAndExecutePreviousIfNeeded(RequestBatch<TBatchReturn, TRequestResponse, TRequestArgument> previousBatch)
    {
        if (previousBatch == null)
        {
            throw new InvalidOperationException("Trying to start null batch which means it was shutdown already.");
        }

        if (Batch.CompareAndSet(previousBatch,
            new RequestBatch<TBatchReturn, TRequestResponse, TRequestArgument>(Properties, _commandCollapser, Properties.MaxRequestsInBatch)))
        {
            previousBatch.ExecuteBatchIfNotAlreadyStarted();
        }
    }
}

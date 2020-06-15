// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.Common.Util;
using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser
{
    public class RequestCollapser<BatchReturnType, RequestResponseType, RequestArgumentType>
    {
        private readonly HystrixCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> commandCollapser;
        private readonly AtomicReference<TimerReference> timerListenerReference = new AtomicReference<TimerReference>();
        private readonly AtomicBoolean timerListenerRegistered = new AtomicBoolean();
        private readonly ICollapserTimer timer;
        private readonly HystrixConcurrencyStrategy concurrencyStrategy;

        public AtomicReference<RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType>> Batch { get; } = new AtomicReference<RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType>>();

        public IHystrixCollapserOptions Properties { get; }

        internal RequestCollapser(HystrixCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> commandCollapser, IHystrixCollapserOptions properties, ICollapserTimer timer, HystrixConcurrencyStrategy concurrencyStrategy)
        {
            // the command with implementation of abstract methods we need
            this.commandCollapser = commandCollapser;
            this.concurrencyStrategy = concurrencyStrategy;
            this.Properties = properties;
            this.timer = timer;
            Batch.Value = new RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType>(properties, commandCollapser, properties.MaxRequestsInBatch);
        }

        public CollapsedRequest<RequestResponseType, RequestArgumentType> SubmitRequest(RequestArgumentType arg, CancellationToken token)
        {
            /*
             * We only want the timer ticking if there are actually things to do so we register it the first time something is added.
             */
            if (!timerListenerRegistered.Value && timerListenerRegistered.CompareAndSet(false, true))
            {
                /* schedule the collapsing task to be executed every x milliseconds (x defined inside CollapsedTask) */
                timerListenerReference.Value = timer.AddListener(new CollapsedTask<BatchReturnType, RequestResponseType, RequestArgumentType>(this));
            }

            // loop until succeed (compare-and-set spin-loop)
            while (true)
            {
                RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType> b = Batch.Value;
                if (b == null)
                {
                    throw new InvalidOperationException("Submitting requests after collapser is shutdown");
                }

                CollapsedRequest<RequestResponseType, RequestArgumentType> response = b.Offer(arg, token);

                // it will always get an CollapsedRequest unless we hit the max batch size
                if (response != null)
                {
                    return response;
                }
                else
                {
                    // this batch can't accept requests so create a new one and set it if another thread doesn't beat us
                    CreateNewBatchAndExecutePreviousIfNeeded(b);
                }
            }
        }

        public void Shutdown()
        {
            RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType> currentBatch = Batch.GetAndSet(null);
            if (currentBatch != null)
            {
                currentBatch.Shutdown();
            }

            if (timerListenerReference.Value != null)
            {
                // if the timer was started we'll clear it so it stops ticking
                timerListenerReference.Value.Dispose();
            }
        }

        internal void CreateNewBatchAndExecutePreviousIfNeeded(RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType> previousBatch)
        {
            if (previousBatch == null)
            {
                throw new InvalidOperationException("Trying to start null batch which means it was shutdown already.");
            }

            if (Batch.CompareAndSet(previousBatch, new RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType>(Properties, commandCollapser, Properties.MaxRequestsInBatch)))
            {
                previousBatch.ExecuteBatchIfNotAlreadyStarted();
            }
        }
    }
}
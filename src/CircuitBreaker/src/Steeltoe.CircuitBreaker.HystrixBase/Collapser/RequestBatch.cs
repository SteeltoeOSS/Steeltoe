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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser
{
    public class RequestBatch<BatchReturnType, RequestResponseType, RequestArgumentType>
    {
        private readonly HystrixCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> commandCollapser;
        private readonly int maxBatchSize;
        private readonly AtomicBoolean batchStarted = new AtomicBoolean();

        private readonly ConcurrentDictionary<RequestArgumentType, CollapsedRequest<RequestResponseType, RequestArgumentType>> argumentMap = new ConcurrentDictionary<RequestArgumentType, CollapsedRequest<RequestResponseType, RequestArgumentType>>();
        private readonly IHystrixCollapserOptions properties;

        private ReaderWriterLockSlim batchLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private AtomicReference<CollapsedRequest<RequestResponseType, RequestArgumentType>> nullArg = new AtomicReference<CollapsedRequest<RequestResponseType, RequestArgumentType>>();

        public RequestBatch(IHystrixCollapserOptions properties, HystrixCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> commandCollapser, int maxBatchSize)
        {
            this.properties = properties;
            this.commandCollapser = commandCollapser;
            this.maxBatchSize = maxBatchSize;
        }

        public CollapsedRequest<RequestResponseType, RequestArgumentType> Offer(RequestArgumentType arg, CancellationToken token)
        {
            /* short-cut - if the batch is started we reject the offer */
            if (batchStarted.Value)
            {
                return null;
            }

            /*
             * The 'read' just means non-exclusive even though we are writing.
             */
            if (batchLock.TryEnterReadLock(1))
            {
                try
                {
                    /* double-check now that we have the lock - if the batch is started we reject the offer */
                    if (batchStarted.Value)
                    {
                        return null;
                    }

                    if (argumentMap.Count >= maxBatchSize)
                    {
                        return null;
                    }
                    else
                    {
                        CollapsedRequest<RequestResponseType, RequestArgumentType> collapsedRequest = new CollapsedRequest<RequestResponseType, RequestArgumentType>(arg, token);
                        TaskCompletionSource<RequestResponseType> tcs = new TaskCompletionSource<RequestResponseType>(collapsedRequest);
                        collapsedRequest.CompletionSource = tcs;

                        CollapsedRequest<RequestResponseType, RequestArgumentType> existing = null;

                        if (arg == null)
                        {
                            existing = GetOrAddNullArg(collapsedRequest);
                        }
                        else
                        {
                            existing = argumentMap.GetOrAdd(arg, collapsedRequest);
                        }

                        /**
                         * If the argument already exists in the batch, then there are 2 options:
                         * A) If request caching is ON (the default): only keep 1 argument in the batch and let all responses
                         * be hooked up to that argument
                         * B) If request caching is OFF: return an error to all duplicate argument requests
                         *
                         * This maintains the invariant that each batch has no duplicate arguments.  This prevents the impossible
                         * logic (in a user-provided mapResponseToRequests for HystrixCollapser and the internals of HystrixObservableCollapser)
                         * of trying to figure out which argument of a set of duplicates should get attached to a response.
                         *
                         * See https://github.com/Netflix/Hystrix/pull/1176 for further discussion.
                         */
                        if (existing != collapsedRequest)
                        {
                            bool requestCachingEnabled = properties.RequestCacheEnabled;
                            if (requestCachingEnabled)
                            {
                                return existing;
                            }
                            else
                            {
                                throw new ArgumentException("Duplicate argument in collapser batch : [" + arg + "]  This is not supported.  Please turn request-caching on for HystrixCollapser:" + commandCollapser.CollapserKey.Name + " or prevent duplicates from making it into the batch!");
                            }
                        }
                        else
                        {
                            return collapsedRequest;
                        }
                    }
                }
                finally
                {
                    batchLock.ExitReadLock();
                }
            }
            else
            {
                return null;
            }
        }

        public void ExecuteBatchIfNotAlreadyStarted()
        {
            /*
             * - check that we only execute once since there's multiple paths to do so (timer, waiting thread or max batch size hit)
             * - close the gate so 'offer' can no longer be invoked and we turn those threads away so they create a new batch
             */
            if (batchStarted.CompareAndSet(false, true))
            {
                /* wait for 'offer'/'remove' threads to finish before executing the batch so 'requests' is complete */
                batchLock.EnterWriteLock();

                List<CollapsedRequest<RequestResponseType, RequestArgumentType>> args = new List<CollapsedRequest<RequestResponseType, RequestArgumentType>>();
                try
                {
                    // Check for cancel
                    foreach (var entry in argumentMap)
                    {
                        if (!entry.Value.IsRequestCanceled())
                        {
                            args.Add(entry.Value);
                        }
                    }

                    // Handle case of null arg submit
                    if (nullArg.Value != null)
                    {
                        var req = nullArg.Value;
                        if (!req.IsRequestCanceled())
                        {
                            args.Add(req);
                        }
                    }

                    if (args.Count > 0)
                    {
                        // shard batches
                        ICollection<ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>>> shards = commandCollapser.DoShardRequests(args);

                        // for each shard execute its requests
                        foreach (ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> shardRequests in shards)
                        {
                            try
                            {
                                // create a new command to handle this batch of requests
                                HystrixCommand<BatchReturnType> command = commandCollapser.DoCreateObservableCommand(shardRequests);
                                BatchReturnType result = command.Execute();

                                try
                                {
                                    commandCollapser.DoMapResponseToRequests(result, shardRequests);
                                }
                                catch (Exception mapException)
                                {
                                    // logger.debug("Exception mapping responses to requests.", e);
                                    foreach (CollapsedRequest<RequestResponseType, RequestArgumentType> request in args)
                                    {
                                        try
                                        {
                                            request.SetExceptionIfResponseNotReceived(mapException);
                                        }
                                        catch (InvalidOperationException)
                                        {
                                            // if we have partial responses set in mapResponseToRequests
                                            // then we may get InvalidOperationException as we loop over them
                                            // so we'll log but continue to the rest
                                            // logger.error("Partial success of 'mapResponseToRequests' resulted in InvalidOperationException while setting Exception. Continuing ... ", e2);
                                        }
                                    }
                                }

                                // check that all requests had setResponse or setException invoked in case 'mapResponseToRequests' was implemented poorly
                                Exception e = null;
                                foreach (CollapsedRequest<RequestResponseType, RequestArgumentType> request in shardRequests)
                                {
                                    try
                                    {
                                        e = request.SetExceptionIfResponseNotReceived(e, "No response set by " + commandCollapser.CollapserKey.Name + " 'mapResponseToRequests' implementation.");
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // logger.debug("Partial success of 'mapResponseToRequests' resulted in InvalidOperationException while setting 'No response set' Exception. Continuing ... ", e2);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                // logger.error("Exception while creating and queueing command with batch.", e);
                                // if a failure occurs we want to pass that exception to all of the Futures that we've returned
                                foreach (CollapsedRequest<RequestResponseType, RequestArgumentType> request in shardRequests)
                                {
                                    try
                                    {
                                        request.Exception = e;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // logger.debug("Failed trying to setException on CollapsedRequest", e2);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // logger.error("Exception while sharding requests.", e);
                    // same error handling as we do around the shards, but this is a wider net in case the shardRequest method fails
                    foreach (ICollapsedRequest<RequestResponseType, RequestArgumentType> request in args)
                    {
                        try
                        {
                            request.Exception = e;
                        }
                        catch (InvalidOperationException)
                        {
                            // logger.debug("Failed trying to setException on CollapsedRequest", e2);
                        }
                    }
                }
                finally
                {
                    batchLock.ExitWriteLock();
                }
            }
        }

        public void Shutdown()
        {
            // take the 'batchStarted' state so offers and execution will not be triggered elsewhere
            if (batchStarted.CompareAndSet(false, true))
            {
                // get the write lock so offers are synced with this (we don't really need to unlock as this is a one-shot deal to shutdown)
                batchLock.EnterWriteLock();
                try
                {
                    // if we win the 'start' and once we have the lock we can now shut it down otherwise another thread will finish executing this batch
                    if (argumentMap.Count > 0)
                    {
                        // logger.warn("Requests still exist in queue but will not be executed due to RequestCollapser shutdown: " + argumentMap.size(), new InvalidOperationException());
                        /*
                         * In the event that there is a concurrency bug or thread scheduling prevents the timer from ticking we need to handle this so the Future.get() calls do not block.
                         *
                         * I haven't been able to reproduce this use case on-demand but when stressing a machine saw this occur briefly right after the JVM paused (logs stopped scrolling).
                         *
                         * This safety-net just prevents the CollapsedRequestFutureImpl.get() from waiting on the CountDownLatch until its max timeout.
                         */
                        foreach (CollapsedRequest<RequestResponseType, RequestArgumentType> request in argumentMap.Values)
                        {
                            try
                            {
                                request.SetExceptionIfResponseNotReceived(new InvalidOperationException("Requests not executed before shutdown."));
                            }
                            catch (Exception)
                            {
                                // logger.debug("Failed to setException on CollapsedRequestFutureImpl instances.", e);
                            }

                            // https://github.com/Netflix/Hystrix/issues/78 Include more info when collapsed requests remain in queue
                            // logger.warn("Request still in queue but not be executed due to RequestCollapser shutdown. Argument => " + request.getArgument() + "   Request Object => " + request, new InvalidOperationException());
                        }
                    }
                }
                finally
                {
                    batchLock.ExitWriteLock();
                }
            }
        }

        public int Size
        {
            get
            {
                var result = argumentMap.Count;
                if (nullArg != null)
                {
                    result++;
                }

                return result;
            }
        }

        internal CollapsedRequest<RequestResponseType, RequestArgumentType> GetOrAddNullArg(CollapsedRequest<RequestResponseType, RequestArgumentType> collapsedRequest)
        {
            if (nullArg.CompareAndSet(null, collapsedRequest))
            {
                return collapsedRequest;
            }

            return nullArg.Value;
        }

        // Best-effort attempt to remove an argument from a batch.  This may get invoked when a cancellation occurs somewhere downstream.
        // This method finds the argument in the batch, and removes it.
        internal void Remove(RequestArgumentType arg)
        {
            if (batchStarted.Value)
            {
                // nothing we can do
                return;
            }

            if (batchLock.TryEnterReadLock(1))
            {
                try
                {
                    /* double-check now that we have the lock - if the batch is started, deleting is useless */
                    if (batchStarted.Value)
                    {
                        return;
                    }

                    if (arg == null)
                    {
                        nullArg.Value = null;
                    }

                    if (argumentMap.TryRemove(arg, out CollapsedRequest<RequestResponseType, RequestArgumentType> existing))
                    {
                        // Log
                    }
                }
                finally
                {
                    batchLock.ExitReadLock();
                }
            }
        }
    }
}

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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class ExecutionResult
    {
        private static readonly IList<HystrixEventType> ALL_EVENT_TYPES = HystrixEventTypeHelper.Values;
        private static readonly int NUM_EVENT_TYPES = ALL_EVENT_TYPES.Count;
        private static readonly BitArray EXCEPTION_PRODUCING_EVENTS = new BitArray(NUM_EVENT_TYPES);
        private static readonly BitArray TERMINAL_EVENTS = new BitArray(NUM_EVENT_TYPES);

        static ExecutionResult()
        {
            foreach (HystrixEventType eventType in HystrixEventTypeHelper.ExceptionProducingEventTypes)
            {
                EXCEPTION_PRODUCING_EVENTS.Set((int)eventType, true);
            }

            foreach (HystrixEventType eventType in HystrixEventTypeHelper.TerminalEventTypes)
            {
                TERMINAL_EVENTS.Set((int)eventType, true);
            }
        }

        public sealed class EventCounts
        {
            private readonly BitArray events;
            private readonly int numEmissions;
            private readonly int numFallbackEmissions;
            private readonly int numCollapsed;

            internal EventCounts()
            {
                events = new BitArray(NUM_EVENT_TYPES);
                numEmissions = 0;
                numFallbackEmissions = 0;
                numCollapsed = 0;
            }

            internal EventCounts(BitArray events, int numEmissions, int numFallbackEmissions, int numCollapsed)
            {
                this.events = events;
                this.numEmissions = numEmissions;
                this.numFallbackEmissions = numFallbackEmissions;
                this.numCollapsed = numCollapsed;
            }

            internal EventCounts(HystrixEventType[] eventTypes)
            {
                BitArray newBitSet = new BitArray(NUM_EVENT_TYPES);
                int localNumEmits = 0;
                int localNumFallbackEmits = 0;
                int localNumCollapsed = 0;
                foreach (HystrixEventType eventType in eventTypes)
                {
                    switch (eventType)
                    {
                        case HystrixEventType.EMIT:
                            newBitSet.Set((int)HystrixEventType.EMIT, true);
                            localNumEmits++;
                            break;
                        case HystrixEventType.FALLBACK_EMIT:
                            newBitSet.Set((int)HystrixEventType.FALLBACK_EMIT, true);
                            localNumFallbackEmits++;
                            break;
                        case HystrixEventType.COLLAPSED:
                            newBitSet.Set((int)HystrixEventType.COLLAPSED, true);
                            localNumCollapsed++;
                            break;
                        default:
                            newBitSet.Set((int)eventType, true);
                            break;
                    }
                }

                events = newBitSet;
                numEmissions = localNumEmits;
                numFallbackEmissions = localNumFallbackEmits;
                numCollapsed = localNumCollapsed;
            }

            public bool Contains(HystrixEventType eventType)
            {
                return events.Get((int)eventType);
            }

            public bool ContainsAnyOf(BitArray other)
            {
                if (other == null)
                {
                    return false;
                }

                for (int i = 0; i < other.Length; i++)
                {
                    if (i >= events.Length)
                    {
                        return false;
                    }

                    if (other[i] && events[i])
                    {
                        return true;
                    }
                }

                return false;
            }

            public int GetCount(HystrixEventType eventType)
            {
                switch (eventType)
                {
                    case HystrixEventType.EMIT: return numEmissions;
                    case HystrixEventType.FALLBACK_EMIT: return numFallbackEmissions;
                    case HystrixEventType.EXCEPTION_THROWN: return ContainsAnyOf(EXCEPTION_PRODUCING_EVENTS) ? 1 : 0;
                    case HystrixEventType.COLLAPSED: return numCollapsed;
                    default: return Contains(eventType) ? 1 : 0;
                }
            }

            public override bool Equals(object o)
            {
                if (this == o)
                {
                    return true;
                }

                if (o == null || GetType() != o.GetType())
                {
                    return false;
                }

                EventCounts that = (EventCounts)o;

                if (numEmissions != that.numEmissions)
                {
                    return false;
                }

                if (numFallbackEmissions != that.numFallbackEmissions)
                {
                    return false;
                }

                if (numCollapsed != that.numCollapsed)
                {
                    return false;
                }

                return Equals(that.events);
            }

            public override int GetHashCode()
            {
                int result = GetHashCode(events);
                result = (31 * result) + numEmissions;
                result = (31 * result) + numFallbackEmissions;
                result = (31 * result) + numCollapsed;
                return result;
            }

            public override string ToString()
            {
                return "EventCounts{" +
                        "events=" + events +
                        ", numEmissions=" + numEmissions +
                        ", numFallbackEmissions=" + numFallbackEmissions +
                        ", numCollapsed=" + numCollapsed +
                        '}';
            }

            internal EventCounts Plus(HystrixEventType eventType)
            {
                return Plus(eventType, 1);
            }

            internal EventCounts Plus(HystrixEventType eventType, int count)
            {
                BitArray newBitSet = new BitArray(events);
                int localNumEmits = numEmissions;
                int localNumFallbackEmits = numFallbackEmissions;
                int localNumCollapsed = numCollapsed;
                switch (eventType)
                {
                    case HystrixEventType.EMIT:
                        newBitSet.Set((int)HystrixEventType.EMIT, true);
                        localNumEmits += count;
                        break;
                    case HystrixEventType.FALLBACK_EMIT:
                        newBitSet.Set((int)HystrixEventType.FALLBACK_EMIT, true);
                        localNumFallbackEmits += count;
                        break;
                    case HystrixEventType.COLLAPSED:
                        newBitSet.Set((int)HystrixEventType.COLLAPSED, true);
                        localNumCollapsed += count;
                        break;
                    default:
                        newBitSet.Set((int)eventType, true);
                        break;
                }

                return new EventCounts(newBitSet, localNumEmits, localNumFallbackEmits, localNumCollapsed);
            }

            private bool Equals(BitArray other)
            {
                if (other.Length != events.Length)
                {
                    return false;
                }

                for (int i = 0; i < events.Length; i++)
                {
                    if (events[i] != other[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            private int GetHashCode(BitArray bits)
            {
                long h = 1234;
                int[] copy = new int[bits.Length];
                ICollection asCollection = bits;
                asCollection.CopyTo(copy, 0);
                for (int i = copy.Length; --i >= 0;)
                {
                    h ^= copy[i] * (i + 1);
                }

                return (int)((h >> 32) ^ h);
            }
        }

        private ExecutionResult(
            EventCounts eventCounts,
            long startTimestamp,
            int executionLatency,
            int userThreadLatency,
            Exception failedExecutionException,
            Exception executionException,
            bool executionOccurred,
            bool isExecutedInThread,
            IHystrixCollapserKey collapserKey)
        {
            Eventcounts = eventCounts;
            StartTimestamp = startTimestamp;
            ExecutionLatency = executionLatency;
            UserThreadLatency = userThreadLatency;
            Exception = failedExecutionException;
            ExecutionException = executionException;
            ExecutionOccurred = executionOccurred;
            IsExecutedInThread = isExecutedInThread;
            CollapserKey = collapserKey;
        }

        // we can return a static version since it's immutable
        internal static readonly ExecutionResult EMPTY = ExecutionResult.From();

        public static ExecutionResult From(params HystrixEventType[] eventTypes)
        {
            bool didExecutionOccur = false;
            foreach (HystrixEventType eventType in eventTypes)
            {
                if (DidExecutionOccur(eventType))
                {
                    didExecutionOccur = true;
                }
            }

            return new ExecutionResult(new EventCounts(eventTypes), -1L, -1, -1, null, null, didExecutionOccur, false, null);
        }

        public ExecutionResult SetExecutionOccurred()
        {
            return new ExecutionResult(
                Eventcounts,
                StartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                Exception,
                ExecutionException,
                true,
                IsExecutedInThread,
                CollapserKey);
        }

        public ExecutionResult SetExecutionLatency(int executionLatency)
        {
            return new ExecutionResult(
                Eventcounts,
                StartTimestamp,
                executionLatency,
                UserThreadLatency,
                Exception,
                ExecutionException,
                ExecutionOccurred,
                IsExecutedInThread,
                CollapserKey);
        }

        public ExecutionResult SetException(Exception e)
        {
            return new ExecutionResult(
                Eventcounts,
                StartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                e,
                ExecutionException,
                ExecutionOccurred,
                IsExecutedInThread,
                CollapserKey);
        }

        public ExecutionResult SetExecutionException(Exception executionException)
        {
            return new ExecutionResult(
                Eventcounts,
                StartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                Exception,
                executionException,
                ExecutionOccurred,
                IsExecutedInThread,
                CollapserKey);
        }

        public ExecutionResult SetInvocationStartTime(long inStartTimestamp)
        {
            return new ExecutionResult(
                Eventcounts,
                inStartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                Exception,
                ExecutionException,
                ExecutionOccurred,
                IsExecutedInThread,
                CollapserKey);
        }

        public ExecutionResult SetExecutedInThread()
        {
            return new ExecutionResult(
                Eventcounts,
                StartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                Exception,
                ExecutionException,
                ExecutionOccurred,
                true,
                CollapserKey);
        }

        public ExecutionResult SetNotExecutedInThread()
        {
            return new ExecutionResult(
                Eventcounts,
                StartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                Exception,
                ExecutionException,
                ExecutionOccurred,
                false,
                CollapserKey);
        }

        public ExecutionResult MarkCollapsed(IHystrixCollapserKey collapserKey, int sizeOfBatch)
        {
            return new ExecutionResult(
                Eventcounts.Plus(HystrixEventType.COLLAPSED, sizeOfBatch),
                StartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                Exception,
                ExecutionException,
                ExecutionOccurred,
                IsExecutedInThread,
                collapserKey);
        }

        public ExecutionResult MarkUserThreadCompletion(long userThreadLatency)
        {
            if (StartTimestamp > 0 && !IsResponseRejected)
            {
                /* execution time (must occur before terminal state otherwise a race condition can occur if requested by client) */
                return new ExecutionResult(
                    Eventcounts,
                    StartTimestamp,
                    ExecutionLatency,
                    (int)userThreadLatency,
                    Exception,
                    ExecutionException,
                    ExecutionOccurred,
                    IsExecutedInThread,
                    CollapserKey);
            }
            else
            {
                return this;
            }
        }

        public ExecutionResult AddEvent(HystrixEventType eventType)
        {
            return new ExecutionResult(
                Eventcounts.Plus(eventType),
                StartTimestamp,
                ExecutionLatency,
                UserThreadLatency,
                Exception,
                ExecutionException,
                ExecutionOccurred,
                IsExecutedInThread,
                CollapserKey);
        }

        public ExecutionResult AddEvent(int executionLatency, HystrixEventType eventType)
        {
            if (StartTimestamp >= 0 && !IsResponseRejected)
            {
                return new ExecutionResult(
                    Eventcounts.Plus(eventType),
                    StartTimestamp,
                    executionLatency,
                    UserThreadLatency,
                    Exception,
                    ExecutionException,
                    ExecutionOccurred,
                    IsExecutedInThread,
                    CollapserKey);
            }
            else
            {
                return AddEvent(eventType);
            }
        }

        public EventCounts Eventcounts { get; }

        public long StartTimestamp { get; }

        /// <summary>
        /// Gets amound of time spent in run() method
        /// </summary>
        public int ExecutionLatency { get; }

        /// <summary>
        /// Gets time elapsed between caller thread submitting request and response being visible to it
        /// </summary>
        public int UserThreadLatency { get; }

        public long CommandRunStartTimeInNanos => StartTimestamp * 1000 * 1000;

        public Exception Exception { get; }

        public Exception ExecutionException { get; }

        public IHystrixCollapserKey CollapserKey { get; }

        public bool IsResponseSemaphoreRejected => Eventcounts.Contains(HystrixEventType.SEMAPHORE_REJECTED);

        public bool IsResponseThreadPoolRejected => Eventcounts.Contains(HystrixEventType.THREAD_POOL_REJECTED);

        public bool IsResponseRejected => IsResponseThreadPoolRejected || IsResponseSemaphoreRejected;

        public List<HystrixEventType> OrderedList
        {
            get
            {
                List<HystrixEventType> eventList = new List<HystrixEventType>();
                foreach (HystrixEventType eventType in ALL_EVENT_TYPES)
                {
                    if (Eventcounts.Contains(eventType))
                    {
                        eventList.Add(eventType);
                    }
                }

                return eventList;
            }
        }

        public bool IsExecutedInThread { get; }

        public bool ExecutionOccurred { get; }

        public bool ContainsTerminalEvent => Eventcounts.ContainsAnyOf(TERMINAL_EVENTS);

        public override string ToString()
        {
            return "ExecutionResult{" +
                    "eventCounts=" + Eventcounts +
                    ", failedExecutionException=" + Exception +
                    ", executionException=" + ExecutionException +
                    ", startTimestamp=" + StartTimestamp +
                    ", executionLatency=" + ExecutionLatency +
                    ", userThreadLatency=" + UserThreadLatency +
                    ", executionOccurred=" + ExecutionOccurred +
                    ", isExecutedInThread=" + IsExecutedInThread +
                    ", collapserKey=" + CollapserKey +
                    '}';
        }

        private static bool DidExecutionOccur(HystrixEventType eventType)
        {
            switch (eventType)
            {
                case HystrixEventType.SUCCESS: return true;
                case HystrixEventType.FAILURE: return true;
                case HystrixEventType.BAD_REQUEST: return true;
                case HystrixEventType.TIMEOUT: return true;
                case HystrixEventType.CANCELLED: return true;
                default: return false;
            }
        }
    }
}
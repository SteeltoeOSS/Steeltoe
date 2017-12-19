// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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

        private readonly EventCounts eventCounts;
        private readonly Exception failedExecutionException;
        private readonly Exception executionException;
        private readonly long startTimestamp;
        private readonly int executionLatency; // time spent in run() method
        private readonly int userThreadLatency; // time elapsed between caller thread submitting request and response being visible to it
        private readonly bool executionOccurred;
        private readonly bool isExecutedInThread;
        private readonly IHystrixCollapserKey collapserKey;

        static ExecutionResult()
        {
            foreach (HystrixEventType eventType in HystrixEventTypeHelper.EXCEPTION_PRODUCING_EVENT_TYPES)
            {
                EXCEPTION_PRODUCING_EVENTS.Set((int)eventType, true);
            }

            foreach (HystrixEventType eventType in HystrixEventTypeHelper.TERMINAL_EVENT_TYPES)
            {
                TERMINAL_EVENTS.Set((int)eventType, true);
            }
        }

        public class EventCounts
        {
            private readonly BitArray events;
            private readonly int numEmissions;
            private readonly int numFallbackEmissions;
            private readonly int numCollapsed;

            internal EventCounts()
            {
                this.events = new BitArray(NUM_EVENT_TYPES);
                this.numEmissions = 0;
                this.numFallbackEmissions = 0;
                this.numCollapsed = 0;
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

                this.events = newBitSet;
                this.numEmissions = localNumEmits;
                this.numFallbackEmissions = localNumFallbackEmits;
                this.numCollapsed = localNumCollapsed;
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

                    if (other[i])
                    {
                        if (events[i])
                        {
                            return true;
                        }
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

                if (o == null || this.GetType() != o.GetType())
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
            this.eventCounts = eventCounts;
            this.startTimestamp = startTimestamp;
            this.executionLatency = executionLatency;
            this.userThreadLatency = userThreadLatency;
            this.failedExecutionException = failedExecutionException;
            this.executionException = executionException;
            this.executionOccurred = executionOccurred;
            this.isExecutedInThread = isExecutedInThread;
            this.collapserKey = collapserKey;
        }

        // we can return a static version since it's immutable
        internal static ExecutionResult EMPTY = ExecutionResult.From();

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
                eventCounts,
                startTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                true,
                isExecutedInThread,
                collapserKey);
        }

        public ExecutionResult SetExecutionLatency(int executionLatency)
        {
            return new ExecutionResult(
                eventCounts,
                startTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                executionOccurred,
                isExecutedInThread,
                collapserKey);
        }

        public ExecutionResult SetException(Exception e)
        {
            return new ExecutionResult(
                eventCounts,
                startTimestamp,
                executionLatency,
                userThreadLatency,
                e,
                executionException,
                executionOccurred,
                isExecutedInThread,
                collapserKey);
        }

        public ExecutionResult SetExecutionException(Exception executionException)
        {
            return new ExecutionResult(
                eventCounts,
                startTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                executionOccurred,
                isExecutedInThread,
                collapserKey);
        }

        public ExecutionResult SetInvocationStartTime(long inStartTimestamp)
        {
            return new ExecutionResult(
                eventCounts,
                inStartTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                executionOccurred,
                isExecutedInThread,
                collapserKey);
        }

        public ExecutionResult SetExecutedInThread()
        {
            return new ExecutionResult(
                eventCounts,
                startTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                executionOccurred,
                true,
                collapserKey);
        }

        public ExecutionResult SetNotExecutedInThread()
        {
            return new ExecutionResult(
                eventCounts,
                startTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                executionOccurred,
                false,
                collapserKey);
        }

        public ExecutionResult MarkCollapsed(IHystrixCollapserKey collapserKey, int sizeOfBatch)
        {
            return new ExecutionResult(
                eventCounts.Plus(HystrixEventType.COLLAPSED, sizeOfBatch),
                startTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                executionOccurred,
                isExecutedInThread,
                collapserKey);
        }

        public ExecutionResult MarkUserThreadCompletion(long userThreadLatency)
        {
            if (startTimestamp > 0 && !IsResponseRejected)
            {
                /* execution time (must occur before terminal state otherwise a race condition can occur if requested by client) */
                return new ExecutionResult(
                    eventCounts,
                    startTimestamp,
                    executionLatency,
                    (int)userThreadLatency,
                    failedExecutionException,
                    executionException,
                    executionOccurred,
                    isExecutedInThread,
                    collapserKey);
            }
            else
            {
                return this;
            }
        }

        public ExecutionResult AddEvent(HystrixEventType eventType)
        {
            return new ExecutionResult(
                eventCounts.Plus(eventType),
                startTimestamp,
                executionLatency,
                userThreadLatency,
                failedExecutionException,
                executionException,
                executionOccurred,
                isExecutedInThread,
                collapserKey);
        }

        public ExecutionResult AddEvent(int executionLatency, HystrixEventType eventType)
        {
            if (startTimestamp >= 0 && !IsResponseRejected)
            {
                return new ExecutionResult(
                    eventCounts.Plus(eventType),
                    startTimestamp,
                    executionLatency,
                    userThreadLatency,
                    failedExecutionException,
                    executionException,
                    executionOccurred,
                    isExecutedInThread,
                    collapserKey);
            }
            else
            {
                return AddEvent(eventType);
            }
        }

        public EventCounts Eventcounts
        {
            get { return eventCounts; }
        }

        public long StartTimestamp
        {
            get { return startTimestamp; }
        }

        public int ExecutionLatency
        {
            get { return executionLatency; }
        }

        public int UserThreadLatency
        {
            get { return userThreadLatency; }
        }

        public long CommandRunStartTimeInNanos
        {
            get { return startTimestamp * 1000 * 1000; }
        }

        public Exception Exception
        {
            get { return failedExecutionException; }
        }

        public Exception ExecutionException
        {
            get { return executionException; }
        }

        public IHystrixCollapserKey CollapserKey
        {
            get { return collapserKey; }
        }

        public bool IsResponseSemaphoreRejected
        {
            get { return eventCounts.Contains(HystrixEventType.SEMAPHORE_REJECTED); }
        }

        public bool IsResponseThreadPoolRejected
        {
            get { return eventCounts.Contains(HystrixEventType.THREAD_POOL_REJECTED); }
        }

        public bool IsResponseRejected
        {
            get { return IsResponseThreadPoolRejected || IsResponseSemaphoreRejected; }
        }

        public List<HystrixEventType> OrderedList
        {
            get
            {
                List<HystrixEventType> eventList = new List<HystrixEventType>();
                foreach (HystrixEventType eventType in ALL_EVENT_TYPES)
                {
                    if (eventCounts.Contains(eventType))
                    {
                        eventList.Add(eventType);
                    }
                }

                return eventList;
            }
        }

        public bool IsExecutedInThread
        {
            get { return this.isExecutedInThread; }
        }

        public bool ExecutionOccurred
        {
            get { return executionOccurred; }
        }

        public bool ContainsTerminalEvent
        {
            get { return eventCounts.ContainsAnyOf(TERMINAL_EVENTS); }
        }

        public override string ToString()
        {
            return "ExecutionResult{" +
                    "eventCounts=" + eventCounts +
                    ", failedExecutionException=" + failedExecutionException +
                    ", executionException=" + executionException +
                    ", startTimestamp=" + startTimestamp +
                    ", executionLatency=" + executionLatency +
                    ", userThreadLatency=" + userThreadLatency +
                    ", executionOccurred=" + executionOccurred +
                    ", isExecutedInThread=" + isExecutedInThread +
                    ", collapserKey=" + collapserKey +
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
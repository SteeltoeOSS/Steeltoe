// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class ExecutionResult
{
    private static readonly IList<HystrixEventType> AllEventTypes = HystrixEventTypeHelper.Values;
    private static readonly int NumEventTypes = AllEventTypes.Count;
    private static readonly BitArray ExceptionProducingEvents = new (NumEventTypes);
    private static readonly BitArray TerminalEvents = new (NumEventTypes);

    static ExecutionResult()
    {
        foreach (var eventType in HystrixEventTypeHelper.ExceptionProducingEventTypes)
        {
            ExceptionProducingEvents.Set((int)eventType, true);
        }

        foreach (var eventType in HystrixEventTypeHelper.TerminalEventTypes)
        {
            TerminalEvents.Set((int)eventType, true);
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

    public sealed class EventCounts
    {
        private readonly BitArray _events;
        private readonly int _numEmissions;
        private readonly int _numFallbackEmissions;
        private readonly int _numCollapsed;

        internal EventCounts()
        {
            _events = new BitArray(NumEventTypes);
            _numEmissions = 0;
            _numFallbackEmissions = 0;
            _numCollapsed = 0;
        }

        internal EventCounts(BitArray events, int numEmissions, int numFallbackEmissions, int numCollapsed)
        {
            _events = events;
            _numEmissions = numEmissions;
            _numFallbackEmissions = numFallbackEmissions;
            _numCollapsed = numCollapsed;
        }

        internal EventCounts(HystrixEventType[] eventTypes)
        {
            var newBitSet = new BitArray(NumEventTypes);
            var localNumEmits = 0;
            var localNumFallbackEmits = 0;
            var localNumCollapsed = 0;
            foreach (var eventType in eventTypes)
            {
                switch (eventType)
                {
                    case HystrixEventType.Emit:
                        newBitSet.Set((int)HystrixEventType.Emit, true);
                        localNumEmits++;
                        break;
                    case HystrixEventType.FallbackEmit:
                        newBitSet.Set((int)HystrixEventType.FallbackEmit, true);
                        localNumFallbackEmits++;
                        break;
                    case HystrixEventType.Collapsed:
                        newBitSet.Set((int)HystrixEventType.Collapsed, true);
                        localNumCollapsed++;
                        break;
                    default:
                        newBitSet.Set((int)eventType, true);
                        break;
                }
            }

            _events = newBitSet;
            _numEmissions = localNumEmits;
            _numFallbackEmissions = localNumFallbackEmits;
            _numCollapsed = localNumCollapsed;
        }

        public bool Contains(HystrixEventType eventType)
        {
            return _events.Get((int)eventType);
        }

        public bool ContainsAnyOf(BitArray other)
        {
            if (other == null)
            {
                return false;
            }

            for (var i = 0; i < other.Length; i++)
            {
                if (i >= _events.Length)
                {
                    return false;
                }

                if (other[i] && _events[i])
                {
                    return true;
                }
            }

            return false;
        }

        public int GetCount(HystrixEventType eventType)
        {
            return eventType switch
            {
                HystrixEventType.Emit => _numEmissions,
                HystrixEventType.FallbackEmit => _numFallbackEmissions,
                HystrixEventType.ExceptionThrown => ContainsAnyOf(ExceptionProducingEvents) ? 1 : 0,
                HystrixEventType.Collapsed => _numCollapsed,
                _ => Contains(eventType) ? 1 : 0,
            };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not EventCounts other)
            {
                return false;
            }

            return _numEmissions == other._numEmissions && _numFallbackEmissions == other._numFallbackEmissions && _numCollapsed == other._numCollapsed && Equals(other._events);
        }

        public override int GetHashCode()
        {
            var eventsHashCode = GetHashCode(_events);
            return HashCode.Combine(eventsHashCode, _numEmissions, _numFallbackEmissions, _numCollapsed);
        }

        public override string ToString()
        {
            return
                $"EventCounts{{events={_events}, numEmissions={_numEmissions}, numFallbackEmissions={_numFallbackEmissions}, numCollapsed={_numCollapsed}}}";
        }

        internal EventCounts Plus(HystrixEventType eventType)
        {
            return Plus(eventType, 1);
        }

        internal EventCounts Plus(HystrixEventType eventType, int count)
        {
            var newBitSet = new BitArray(_events);
            var localNumEmits = _numEmissions;
            var localNumFallbackEmits = _numFallbackEmissions;
            var localNumCollapsed = _numCollapsed;
            switch (eventType)
            {
                case HystrixEventType.Emit:
                    newBitSet.Set((int)HystrixEventType.Emit, true);
                    localNumEmits += count;
                    break;
                case HystrixEventType.FallbackEmit:
                    newBitSet.Set((int)HystrixEventType.FallbackEmit, true);
                    localNumFallbackEmits += count;
                    break;
                case HystrixEventType.Collapsed:
                    newBitSet.Set((int)HystrixEventType.Collapsed, true);
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
            if (other.Length != _events.Length)
            {
                return false;
            }

            for (var i = 0; i < _events.Length; i++)
            {
                if (_events[i] != other[i])
                {
                    return false;
                }
            }

            return true;
        }

        private int GetHashCode(BitArray bits)
        {
            long h = 1234;
            var copy = new int[bits.Length];
            ICollection asCollection = bits;
            asCollection.CopyTo(copy, 0);
            for (var i = copy.Length; --i >= 0;)
            {
                h ^= copy[i] * (i + 1);
            }

            return (int)((h >> 32) ^ h);
        }
    }

    public static ExecutionResult From(params HystrixEventType[] eventTypes)
    {
        var didExecutionOccur = false;
        foreach (var eventType in eventTypes)
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
            Eventcounts.Plus(HystrixEventType.Collapsed, sizeOfBatch),
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
    /// Gets amount of time spent in run() method.
    /// </summary>
    public int ExecutionLatency { get; }

    /// <summary>
    /// Gets time elapsed between caller thread submitting request and response being visible to it.
    /// </summary>
    public int UserThreadLatency { get; }

    public long CommandRunStartTimeInNanoseconds => StartTimestamp * 1000 * 1000;

    public Exception Exception { get; }

    public Exception ExecutionException { get; }

    public IHystrixCollapserKey CollapserKey { get; }

    public bool IsResponseSemaphoreRejected => Eventcounts.Contains(HystrixEventType.SemaphoreRejected);

    public bool IsResponseThreadPoolRejected => Eventcounts.Contains(HystrixEventType.ThreadPoolRejected);

    public bool IsResponseRejected => IsResponseThreadPoolRejected || IsResponseSemaphoreRejected;

    public List<HystrixEventType> OrderedList
    {
        get
        {
            var eventList = new List<HystrixEventType>();
            foreach (var eventType in AllEventTypes)
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

    public bool ContainsTerminalEvent => Eventcounts.ContainsAnyOf(TerminalEvents);

    public override string ToString()
    {
        return
            $"ExecutionResult{{eventCounts={Eventcounts}, failedExecutionException={Exception}, executionException={ExecutionException}, startTimestamp={StartTimestamp}, executionLatency={ExecutionLatency}, userThreadLatency={UserThreadLatency}, executionOccurred={ExecutionOccurred}, isExecutedInThread={IsExecutedInThread}, collapserKey={CollapserKey}}}";
    }

    private static bool DidExecutionOccur(HystrixEventType eventType)
    {
        return eventType switch
        {
            HystrixEventType.Success => true,
            HystrixEventType.Failure => true,
            HystrixEventType.BadRequest => true,
            HystrixEventType.Timeout => true,
            HystrixEventType.Cancelled => true,
            _ => false,
        };
    }
}

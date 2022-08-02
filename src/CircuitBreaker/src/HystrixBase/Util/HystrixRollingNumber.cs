// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public class HystrixRollingNumber
{
    private static readonly ITime ActualTime = new ActualTime();
    private readonly ITime _time;

    private readonly object _newBucketLock = new();
    internal readonly int TimeInMilliseconds;
    internal readonly int NumberOfBuckets;
    internal readonly int BucketSizeInMilliseconds;

    internal readonly BucketCircularArray Buckets;
    internal readonly CumulativeSum DefaultCumulativeSum = new();

    public HystrixRollingNumber(int timeInMilliseconds, int numberOfBuckets)
        : this(ActualTime, timeInMilliseconds, numberOfBuckets)
    {
    }

    /* package for testing */
    internal HystrixRollingNumber(ITime time, int timeInMilliseconds, int numberOfBuckets)
    {
        _time = time;
        TimeInMilliseconds = timeInMilliseconds;
        NumberOfBuckets = numberOfBuckets;

        if (timeInMilliseconds % numberOfBuckets != 0)
        {
            throw new ArgumentException("The timeInMilliseconds must divide equally into numberOfBuckets. For example 1000/10 is ok, 1000/11 is not.");
        }

        BucketSizeInMilliseconds = timeInMilliseconds / numberOfBuckets;

        Buckets = new BucketCircularArray(numberOfBuckets);
    }

    public void Increment(HystrixRollingNumberEvent type)
    {
        GetCurrentBucket().GetAdder(type).Increment();
    }

    public void Add(HystrixRollingNumberEvent type, long value)
    {
        GetCurrentBucket().GetAdder(type).Add(value);
    }

    public void UpdateRollingMax(HystrixRollingNumberEvent type, long value)
    {
        GetCurrentBucket().GetMaxUpdater(type).Update(value);
    }

    public void Reset()
    {
        // if we are resetting, that means the lastBucket won't have a chance to be captured in CumulativeSum, so let's do it here
        Bucket lastBucket = Buckets.PeekLast;

        if (lastBucket != null)
        {
            DefaultCumulativeSum.AddBucket(lastBucket);
        }

        // clear buckets so we start over again
        Buckets.Clear();
    }

    public long GetCumulativeSum(HystrixRollingNumberEvent type)
    {
        // this isn't 100% atomic since multiple threads can be affecting latestBucket & cumulativeSum independently
        // but that's okay since the count is always a moving target and we're accepting a "point in time" best attempt
        // we are however putting 'getValueOfLatestBucket' first since it can have side-affects on cumulativeSum whereas the inverse is not true
        return GetValueOfLatestBucket(type) + DefaultCumulativeSum.Get(type);
    }

    public long GetRollingSum(HystrixRollingNumberEvent type)
    {
        Bucket lastBucket = GetCurrentBucket();

        if (lastBucket == null)
        {
            return 0;
        }

        long sum = 0;

        foreach (Bucket b in Buckets)
        {
            sum += b.GetAdder(type).Sum();
        }

        return sum;
    }

    public long GetValueOfLatestBucket(HystrixRollingNumberEvent type)
    {
        Bucket lastBucket = GetCurrentBucket();

        if (lastBucket == null)
        {
            return 0;
        }

        // we have bucket data so we'll return the lastBucket
        return lastBucket.Get(type);
    }

    public long[] GetValues(HystrixRollingNumberEvent type)
    {
        Bucket lastBucket = GetCurrentBucket();

        if (lastBucket == null)
        {
            return Array.Empty<long>();
        }

        // get buckets as an array (which is a copy of the current state at this point in time)
        Bucket[] bucketArray = Buckets.Array;

        // we have bucket data so we'll return an array of values for all buckets
        long[] values = new long[bucketArray.Length];
        int i = 0;

        foreach (Bucket bucket in bucketArray)
        {
            if (HystrixRollingNumberEventHelper.IsCounter(type))
            {
                values[i++] = bucket.GetAdder(type).Sum();
            }
            else if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
            {
                values[i++] = bucket.GetMaxUpdater(type).Max;
            }
        }

        return values;
    }

    public long GetRollingMaxValue(HystrixRollingNumberEvent type)
    {
        long[] values = GetValues(type);

        if (values.Length == 0)
        {
            return 0;
        }

        Array.Sort(values);
        return values[values.Length - 1];
    }

    /* package for testing */
    internal Bucket GetCurrentBucket()
    {
        long currentTime = _time.CurrentTimeInMillis;

        /* a shortcut to try and get the most common result of immediately finding the current bucket */

        /*
         * Retrieve the latest bucket if the given time is BEFORE the end of the bucket window, otherwise it returns NULL.
         * NOTE: This is thread-safe because it's accessing 'buckets' which is a LinkedBlockingDeque
         */
        Bucket currentBucket = Buckets.PeekLast;

        if (currentBucket != null && currentTime < currentBucket.WindowStart + BucketSizeInMilliseconds)
        {
            // if we're within the bucket 'window of time' return the current one
            // NOTE: We do not worry if we are BEFORE the window in a weird case of where thread scheduling causes that to occur,
            // we'll just use the latest as long as we're not AFTER the window
            return currentBucket;
        }

        /* if we didn't find the current bucket above, then we have to create one */

        /*
         * The following needs to be synchronized/locked even with a synchronized/thread-safe data structure such as LinkedBlockingDeque because
         * the logic involves multiple steps to check existence, create an object then insert the object. The 'check' or 'insertion' themselves
         * are thread-safe by themselves but not the aggregate algorithm, thus we put this entire block of logic inside synchronized.
         * I am using a tryLock if/then (https://download.oracle.com/javase/6/docs/api/java/util/concurrent/locks/Lock.html#tryLock())
         * so that a single thread will get the lock and as soon as one thread gets the lock all others will go the 'else' block
         * and just return the currentBucket until the newBucket is created. This should allow the throughput to be far higher
         * and only slow down 1 thread instead of blocking all of them in each cycle of creating a new bucket based on some testing
         * (and it makes sense that it should as well).
         * This means the timing won't be exact to the millisecond as to what data ends up in a bucket, but that's acceptable.
         * It's not critical to have exact precision to the millisecond, as long as it's rolling, if we can instead reduce the impact synchronization.
         * More importantly though it means that the 'if' block within the lock needs to be careful about what it changes that can still
         * be accessed concurrently in the 'else' block since we're not completely synchronizing access.
         * For example, we can't have a multi-step process to add a bucket, remove a bucket, then update the sum since the 'else' block of code
         * can retrieve the sum while this is all happening. The trade-off is that we don't maintain the rolling sum and let readers just iterate
         * bucket to calculate the sum themselves. This is an example of favoring write-performance instead of read-performance and how the tryLock
         * versus a synchronized block needs to be accommodated.
         */
        bool lockTaken = false;
        Monitor.TryEnter(_newBucketLock, ref lockTaken);

        if (lockTaken)
        {
            currentTime = _time.CurrentTimeInMillis;

            try
            {
                if (Buckets.PeekLast == null)
                {
                    // the list is empty so create the first bucket
                    var newBucket = new Bucket(currentTime);
                    Buckets.AddLast(newBucket);
                    return newBucket;
                }
                else
                {
                    // We go into a loop so that it will create as many buckets as needed to catch up to the current time
                    // as we want the buckets complete even if we don't have transactions during a period of time.
                    for (int i = 0; i < NumberOfBuckets; i++)
                    {
                        // we have at least 1 bucket so retrieve it
                        Bucket lastBucket = Buckets.PeekLast;

                        if (currentTime < lastBucket.WindowStart + BucketSizeInMilliseconds)
                        {
                            // if we're within the bucket 'window of time' return the current one
                            // NOTE: We do not worry if we are BEFORE the window in a weird case of where thread scheduling causes that to occur,
                            // we'll just use the latest as long as we're not AFTER the window
                            return lastBucket;
                        }
                        else if (currentTime - (lastBucket.WindowStart + BucketSizeInMilliseconds) > TimeInMilliseconds)
                        {
                            // the time passed is greater than the entire rolling counter so we want to clear it all and start from scratch
                            Reset();

                            var newBucket = new Bucket(currentTime);
                            Buckets.AddLast(newBucket);
                            return newBucket;
                        }
                        else
                        {
                            // we're past the window so we need to create a new bucket
                            // create a new bucket and add it as the new 'last'
                            Buckets.AddLast(new Bucket(lastBucket.WindowStart + BucketSizeInMilliseconds));

                            // add the lastBucket values to the cumulativeSum
                            DefaultCumulativeSum.AddBucket(lastBucket);
                        }
                    }

                    // we have finished the for-loop and created all of the buckets, so return the lastBucket now
                    return Buckets.PeekLast;
                }
            }
            finally
            {
                Monitor.Exit(_newBucketLock);
            }
        }

        currentBucket = Buckets.PeekLast;

        if (currentBucket != null)
        {
            // we didn't get the lock so just return the latest bucket while another thread creates the next one
            return currentBucket;
        }

        // the rare scenario where multiple threads raced to create the very first bucket
        // wait slightly and then use recursion while the other thread finishes creating a bucket
        if (Time.WaitUntil(() => Buckets.PeekLast != null, 500))
        {
            return Buckets.PeekLast;
        }

        return null;
    }

    internal sealed class Bucket
    {
        internal readonly long WindowStart;
        internal readonly LongAdder[] AdderForCounterType;
        internal readonly LongMaxUpdater[] UpdaterForCounterType;

        public Bucket(long startTime)
        {
            WindowStart = startTime;

            /*
             * We support both LongAdder and LongMaxUpdater in a bucket but don't want the memory allocation
             * of all types for each so we only allocate the objects if the HystrixRollingNumberEvent matches
             * the correct type - though we still have the allocation of empty arrays to the given length
             * as we want to keep using the type.ordinal() value for fast random access.
             */

            // initialize the array of LongAdders
            AdderForCounterType = new LongAdder[HystrixRollingNumberEventHelper.Values.Count];

            foreach (HystrixRollingNumberEvent type in HystrixRollingNumberEventHelper.Values)
            {
                if (HystrixRollingNumberEventHelper.IsCounter(type))
                {
                    AdderForCounterType[(int)type] = new LongAdder();
                }
            }

            UpdaterForCounterType = new LongMaxUpdater[HystrixRollingNumberEventHelper.Values.Count];

            foreach (HystrixRollingNumberEvent type in HystrixRollingNumberEventHelper.Values)
            {
                if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                {
                    UpdaterForCounterType[(int)type] = new LongMaxUpdater();

                    // initialize to 0 otherwise it is Long.MIN_VALUE
                    UpdaterForCounterType[(int)type].Update(0);
                }
            }
        }

        public long Get(HystrixRollingNumberEvent type)
        {
            if (HystrixRollingNumberEventHelper.IsCounter(type))
            {
                return AdderForCounterType[(int)type].Sum();
            }

            if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
            {
                return UpdaterForCounterType[(int)type].Max;
            }

            throw new InvalidOperationException($"Unknown type of event: {type}");
        }

        public LongAdder GetAdder(HystrixRollingNumberEvent type)
        {
            if (!HystrixRollingNumberEventHelper.IsCounter(type))
            {
                throw new InvalidOperationException($"Type is not a Counter: {type}");
            }

            return AdderForCounterType[(int)type];
        }

        public LongMaxUpdater GetMaxUpdater(HystrixRollingNumberEvent type)
        {
            if (!HystrixRollingNumberEventHelper.IsMaxUpdater(type))
            {
                throw new InvalidOperationException($"Type is not a MaxUpdater: {type}");
            }

            return UpdaterForCounterType[(int)type];
        }
    }

    internal sealed class CumulativeSum
    {
        internal readonly LongAdder[] AdderForCounterType;
        internal readonly LongMaxUpdater[] UpdaterForCounterType;

        public CumulativeSum()
        {
            /*
             * We support both LongAdder and LongMaxUpdater in a bucket but don't want the memory allocation
             * of all types for each so we only allocate the objects if the HystrixRollingNumberEvent matches
             * the correct type - though we still have the allocation of empty arrays to the given length
             * as we want to keep using the type.ordinal() value for fast random access.
             */

            // initialize the array of LongAdders
            AdderForCounterType = new LongAdder[HystrixRollingNumberEventHelper.Values.Count];

            foreach (HystrixRollingNumberEvent type in HystrixRollingNumberEventHelper.Values)
            {
                if (HystrixRollingNumberEventHelper.IsCounter(type))
                {
                    AdderForCounterType[(int)type] = new LongAdder();
                }
            }

            UpdaterForCounterType = new LongMaxUpdater[HystrixRollingNumberEventHelper.Values.Count];

            foreach (HystrixRollingNumberEvent type in HystrixRollingNumberEventHelper.Values)
            {
                if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                {
                    UpdaterForCounterType[(int)type] = new LongMaxUpdater();

                    // initialize to 0 otherwise it is Long.MIN_VALUE
                    UpdaterForCounterType[(int)type].Update(0);
                }
            }
        }

        public void AddBucket(Bucket lastBucket)
        {
            foreach (HystrixRollingNumberEvent type in HystrixRollingNumberEventHelper.Values)
            {
                if (HystrixRollingNumberEventHelper.IsCounter(type))
                {
                    GetAdder(type).Add(lastBucket.GetAdder(type).Sum());
                }

                if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                {
                    GetMaxUpdater(type).Update(lastBucket.GetMaxUpdater(type).Max);
                }
            }
        }

        public long Get(HystrixRollingNumberEvent type)
        {
            if (HystrixRollingNumberEventHelper.IsCounter(type))
            {
                return AdderForCounterType[(int)type].Sum();
            }

            if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
            {
                return UpdaterForCounterType[(int)type].Max;
            }

            throw new InvalidOperationException($"Unknown type of event: {type}");
        }

        public LongAdder GetAdder(HystrixRollingNumberEvent type)
        {
            if (!HystrixRollingNumberEventHelper.IsCounter(type))
            {
                throw new InvalidOperationException($"Type is not a Counter: {type}");
            }

            return AdderForCounterType[(int)type];
        }

        public LongMaxUpdater GetMaxUpdater(HystrixRollingNumberEvent type)
        {
            if (!HystrixRollingNumberEventHelper.IsMaxUpdater(type))
            {
                throw new InvalidOperationException($"Type is not a MaxUpdater: {type}");
            }

            return UpdaterForCounterType[(int)type];
        }
    }

    internal sealed class BucketCircularArray : IEnumerable<Bucket>
    {
        private readonly AtomicReference<ListState> _state;
        private readonly int _dataLength; // we don't resize, we always stay the same, so remember this
        private readonly int _numBuckets;

        public Bucket Last => PeekLast;

        public int Size =>
            // the size can also be worked out each time as:
            // return (tail + data.length() - head) % data.length();
            _state.Value.Size;

        public Bucket PeekLast => _state.Value.Tail;

        public Bucket[] Array => _state.Value.Array;

        public BucketCircularArray(int size)
        {
            var buckets = new AtomicReferenceArray<Bucket>(size + 1); // + 1 as extra room for the add/remove;
            _state = new AtomicReference<ListState>(new ListState(this, buckets, 0, 0));
            _dataLength = buckets.Length;
            _numBuckets = size;
        }

        public void Clear()
        {
            while (true)
            {
                /*
                 * it should be very hard to not succeed the first pass thru since this is typically is only called from
                 * a single thread protected by a tryLock, but there is at least 1 other place (at time of writing this comment)
                 * where reset can be called from (CircuitBreaker.markSuccess after circuit was tripped) so it can
                 * in an edge-case conflict.
                 * Instead of trying to determine if someone already successfully called clear() and we should skip
                 * we will have both calls reset the circuit, even if that means losing data added in between the two
                 * depending on thread scheduling.
                 * The rare scenario in which that would occur, we'll accept the possible data loss while clearing it
                 * since the code has stated its desire to clear() anyways.
                 */
                ListState current = _state.Value;
                ListState newState = current.Clear();

                if (_state.CompareAndSet(current, newState))
                {
                    return;
                }
            }
        }

        public void AddLast(Bucket o)
        {
            ListState currentState = _state.Value;

            // create new version of state (what we want it to become)
            ListState newState = currentState.AddBucket(o);

            /*
             * use compareAndSet to set in case multiple threads are attempting (which shouldn't be the case because since addLast will ONLY be called by a single thread at a time due to protection
             * provided in <code>getCurrentBucket</code>)
             */
#pragma warning disable S3923 // All branches in a conditional structure should not have exactly the same implementation
            if (_state.CompareAndSet(currentState, newState))
            {
                // we succeeded
            }
#pragma warning restore S3923 // All branches in a conditional structure should not have exactly the same implementation
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Bucket> GetEnumerator()
        {
            var list = new List<Bucket>(Array);
            return list.AsReadOnly().GetEnumerator();
        }

        internal sealed class ListState
        {
            /*
                * this is an AtomicReferenceArray and not a normal Array because we're copying the reference
                * between ListState objects and multiple threads could maintain references across these
                * compound operations so I want the visibility/concurrency guarantees
                */
            internal readonly AtomicReferenceArray<Bucket> Data;
            internal readonly int Size;
            internal readonly int ListTail;
            internal readonly int Head;
            internal readonly BucketCircularArray Ca;

            public Bucket Tail
            {
                get
                {
                    if (Size == 0)
                    {
                        return null;
                    }

                    // we want to get the last item, so size()-1
                    return Data[Convert(Size - 1)];
                }
            }

            public Bucket[] Array
            {
                get
                {
                    /*
                     * this isn't technically thread-safe since it requires multiple reads on something that can change
                     * but since we never clear the data directly, only increment/decrement head/tail we would never get a NULL
                     * just potentially return stale data which we are okay with doing
                     */
                    var array = new List<Bucket>();

                    for (int i = 0; i < Size; i++)
                    {
                        array.Add(Data[Convert(i)]);
                    }

                    return array.ToArray();
                }
            }

            public ListState(BucketCircularArray ca, AtomicReferenceArray<Bucket> data, int head, int tail)
            {
                Ca = ca;
                Head = head;
                ListTail = tail;

                if (head == 0 && tail == 0)
                {
                    Size = 0;
                }
                else
                {
                    Size = (tail + ca._dataLength - head) % ca._dataLength;
                }

                Data = data;
            }

            public ListState Clear()
            {
                return new ListState(Ca, new AtomicReferenceArray<Bucket>(Ca._dataLength), 0, 0);
            }

            public ListState AddBucket(Bucket b)
            {
                /*
                 * We could in theory have 2 threads addBucket concurrently and this compound operation would interleave.
                 * This should NOT happen since getCurrentBucket is supposed to be executed by a single thread.
                 * If it does happen, it's not a huge deal as incrementTail() will be protected by compareAndSet and one of the two addBucket calls will succeed with one of the Buckets.
                 * In either case, a single Bucket will be returned as "last" and data loss should not occur and everything keeps in sync for head/tail.
                 * Also, it's fine to set it before incrementTail because nothing else should be referencing that index position until incrementTail occurs.
                 */
                Data[ListTail] = b;
                return IncrementTail();
            }

            // The convert() method takes a logical index (as if head was
            // always 0) and calculates the index within elementData
            private int Convert(int index)
            {
                return (index + Head) % Ca._dataLength;
            }

            private ListState IncrementTail()
            {
                /* if incrementing results in growing larger than 'length' which is the max we should be at, then also increment head (equivalent of removeFirst but done atomically) */
                if (Size == Ca._numBuckets)
                {
                    // increment tail and head
                    return new ListState(Ca, Data, (Head + 1) % Ca._dataLength, (ListTail + 1) % Ca._dataLength);
                }

                // increment only tail
                return new ListState(Ca, Data, Head, (ListTail + 1) % Ca._dataLength);
            }
        }
    }
}

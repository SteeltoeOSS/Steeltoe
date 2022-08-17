// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public class HystrixRollingPercentile
{
    private static readonly ITime ActualTime = new ActualTime();
    private readonly ITime _time;

    private readonly object _newBucketLock = new();
    internal readonly BucketCircularArray Buckets;
    internal readonly int TimeInMilliseconds;
    internal readonly int NumberOfBuckets;
    internal readonly int BucketDataLength;
    internal readonly int BucketSizeInMilliseconds;
    internal readonly bool Enabled;

    /*
     * This will get flipped each time a new bucket is created.
     */
    /* package for testing */
    private volatile PercentileSnapshot _currentPercentileSnapshot = new(0);

    private PercentileSnapshot CurrentPercentileSnapshot => _currentPercentileSnapshot;

    public int Mean
    {
        get
        {
            /* no-op if disabled */
            if (!Enabled)
            {
                return -1;
            }

            // force logic to move buckets forward in case other requests aren't making it happen
            GetCurrentBucket();

            // fetch the current snapshot
            return CurrentPercentileSnapshot.Mean;
        }
    }

    public HystrixRollingPercentile(int timeInMilliseconds, int numberOfBuckets, int bucketDataLength, bool enabled)
        : this(ActualTime, timeInMilliseconds, numberOfBuckets, bucketDataLength, enabled)
    {
    }

    /* package for testing */
    internal HystrixRollingPercentile(ITime time, int timeInMilliseconds, int numberOfBuckets, int bucketDataLength, bool enabled)
    {
        _time = time;
        TimeInMilliseconds = timeInMilliseconds;
        NumberOfBuckets = numberOfBuckets;
        BucketDataLength = bucketDataLength;
        Enabled = enabled;

        if (TimeInMilliseconds % NumberOfBuckets != 0)
        {
            throw new ArgumentException("The time must divide equally into the number of buckets. For example 1000/10 is ok, 1000/11 is not.",
                nameof(timeInMilliseconds));
        }

        BucketSizeInMilliseconds = TimeInMilliseconds / NumberOfBuckets;

        Buckets = new BucketCircularArray(NumberOfBuckets);
    }

    public void AddValue(params int[] value)
    {
        /* no-op if disabled */
        if (!Enabled)
        {
            return;
        }

        foreach (int v in value)
        {
            try
            {
                Bucket currentBucket = GetCurrentBucket();

                if (currentBucket != null)
                {
                    currentBucket.Data.AddValue(v);
                }
            }
            catch (Exception)
            {
                // logger.error("Failed to add value: " + v, e);
            }
        }
    }

    public int GetPercentile(double percentile)
    {
        /* no-op if disabled */
        if (!Enabled)
        {
            return -1;
        }

        // force logic to move buckets forward in case other requests aren't making it happen
        GetCurrentBucket();

        // fetch the current snapshot
        return CurrentPercentileSnapshot.GetPercentile(percentile);
    }

    public void Reset()
    {
        /* no-op if disabled */
        if (!Enabled)
        {
            return;
        }

        // clear buckets so we start over again
        Buckets.Clear();

        // and also make sure the percentile snapshot gets reset
        _currentPercentileSnapshot = new PercentileSnapshot(Buckets.Array);
    }

    private Bucket GetCurrentBucket()
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
                    var newBucket = new Bucket(currentTime, BucketDataLength);
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

                            // recursively call getCurrentBucket which will create a new bucket and return it
                            // return GetCurrentBucket();
                            var newBucket = new Bucket(currentTime, BucketDataLength);
                            Buckets.AddLast(newBucket);
                            return newBucket;
                        }
                        else
                        {
                            // we're past the window so we need to create a new bucket
                            Bucket[] allBuckets = Buckets.Array;

                            // create a new bucket and add it as the new 'last' (once this is done other threads will start using it on subsequent retrievals)
                            Buckets.AddLast(new Bucket(lastBucket.WindowStart + BucketSizeInMilliseconds, BucketDataLength));

                            // we created a new bucket so let's re-generate the PercentileSnapshot (not including the new bucket)
                            _currentPercentileSnapshot = new PercentileSnapshot(allBuckets);
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

    internal sealed class PercentileBucketData
    {
        internal readonly int DataLength;
        internal readonly AtomicIntegerArray List;
        internal readonly AtomicInteger Index = new();

        public int Length
        {
            get
            {
                if (Index.Value > List.Length)
                {
                    return List.Length;
                }

                return Index.Value;
            }
        }

        public PercentileBucketData(int dataLength)
        {
            DataLength = dataLength;
            List = new AtomicIntegerArray(dataLength);
        }

        public void AddValue(params int[] latency)
        {
            foreach (int l in latency)
            {
                /* We just wrap around the beginning and over-write if we go past 'dataLength' as that will effectively cause us to "sample" the most recent data */
                List[Index.GetAndIncrement() % DataLength] = l;

                // TODO Alternative to AtomicInteger? The getAndIncrement may be a source of contention on high throughput circuits on large multi-core systems.
                // LongAdder isn't suited to this as it is not consistent. Perhaps a different data structure that doesn't need indexed adds?
                // A threadlocal data storage that only aggregates when fetched would be ideal. Similar to LongAdder except for accumulating lists of data.
            }
        }
    }

    internal sealed class PercentileSnapshot
    {
        private readonly int[] _data;
        private readonly int _length;

        /* package for testing */
        public int Mean { get; }

        /* package for testing */
        public PercentileSnapshot(Bucket[] buckets)
        {
            int lengthFromBuckets = 0;

            // we need to calculate it dynamically as it could have been changed by properties (rare, but possible)
            // also this way we capture the actual index size rather than the max so size the int[] to only what we need
            foreach (Bucket bd in buckets)
            {
                lengthFromBuckets += bd.Data.DataLength;
            }

            _data = new int[lengthFromBuckets];
            int index = 0;
            int sum = 0;

            foreach (Bucket bd in buckets)
            {
                PercentileBucketData pbd = bd.Data;
                int pbdLength = pbd.Length;

                for (int i = 0; i < pbdLength; i++)
                {
                    int v = pbd.List[i];
                    _data[index++] = v;
                    sum += v;
                }
            }

            _length = index;

            if (_length == 0)
            {
                Mean = 0;
            }
            else
            {
                Mean = sum / _length;
            }

            Array.Sort(_data, 0, _length);
        }

        /* package for testing */
        public PercentileSnapshot(params int[] data)
        {
            _data = data;
            _length = data.Length;

            int sum = 0;

            foreach (int v in data)
            {
                sum += v;
            }

            Mean = sum / _length;

            Array.Sort(_data, 0, _length);
        }

        public int GetPercentile(double percentile)
        {
            if (_length == 0)
            {
                return 0;
            }

            return ComputePercentile(percentile);
        }

        private int ComputePercentile(double percent)
        {
            // Some just-in-case edge cases
            if (_length <= 0)
            {
                return 0;
            }

            if (percent <= 0.0)
            {
                return _data[0];
            }

            if (percent >= 100.0)
            {
                return _data[_length - 1];
            }

            // ranking (https://en.wikipedia.org/wiki/Percentile#Alternative_methods)
            double rank = percent / 100.0 * _length;

            // linear interpolation between closest ranks
            int iLow = (int)Math.Floor(rank);
            int iHigh = (int)Math.Ceiling(rank);

            // assert 0 <= iLow && iLow <= rank && rank <= iHigh && iHigh <= length;
            // assert(iHigh - iLow) <= 1;
            if (iHigh >= _length)
            {
                // Another edge case
                return _data[_length - 1];
            }

            if (iLow == iHigh)
            {
                return _data[iLow];
            }

            // Interpolate between the two bounding values
            return (int)(_data[iLow] + (rank - iLow) * (_data[iHigh] - _data[iLow]));
        }
    }

    internal sealed class BucketCircularArray : IEnumerable<Bucket>
    {
        private readonly AtomicReference<ListState> _state;
        private readonly int _dataLength; // we don't resize, we always stay the same, so remember this
        private readonly int _numBuckets;

        // the size can also be worked out each time as:
        // return (tail + data.length() - head) % data.length();
        public int Size => _state.Value.Size;

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
            if (_state.CompareAndSet(currentState, newState))
            {
                // we succeeded
            }
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
            internal readonly int BucketTail;
            internal readonly int Head;
            internal BucketCircularArray Cb;

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

            public ListState(BucketCircularArray cb, AtomicReferenceArray<Bucket> data, int head, int tail)
            {
                Cb = cb;
                Head = head;
                BucketTail = tail;

                if (head == 0 && tail == 0)
                {
                    Size = 0;
                }
                else
                {
                    Size = (tail + cb._dataLength - head) % cb._dataLength;
                }

                Data = data;
            }

            public ListState Clear()
            {
                return new ListState(Cb, new AtomicReferenceArray<Bucket>(Cb._dataLength), 0, 0);
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
                Data[BucketTail] = b;
                return IncrementTail();
            }

            // The convert() method takes a logical index (as if head was
            // always 0) and calculates the index within elementData
            private int Convert(int index)
            {
                return (index + Head) % Cb._dataLength;
            }

            private ListState IncrementTail()
            {
                /* if incrementing results in growing larger than 'length' which is the max we should be at, then also increment head (equivalent of removeFirst but done atomically) */
                if (Size == Cb._numBuckets)
                {
                    // increment tail and head
                    return new ListState(Cb, Data, (Head + 1) % Cb._dataLength, (BucketTail + 1) % Cb._dataLength);
                }

                // increment only tail
                return new ListState(Cb, Data, Head, (BucketTail + 1) % Cb._dataLength);
            }
        }
    }

    internal sealed class Bucket
    {
        internal readonly long WindowStart;
        internal readonly PercentileBucketData Data;

        public Bucket(long startTime, int bucketDataLength)
        {
            WindowStart = startTime;
            Data = new PercentileBucketData(bucketDataLength);
        }
    }
}

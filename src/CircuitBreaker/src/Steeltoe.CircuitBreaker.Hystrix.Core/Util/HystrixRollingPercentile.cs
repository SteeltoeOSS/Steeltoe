//
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
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class HystrixRollingPercentile
    {

        //private static readonly Logger logger = LoggerFactory.getLogger(HystrixRollingPercentile.class);

        private static ITime ACTUAL_TIME = new ActualTime();
        private readonly ITime time;
        internal readonly BucketCircularArray buckets;
        internal readonly int timeInMilliseconds;
        internal readonly int numberOfBuckets;
        internal readonly int bucketDataLength;
        internal readonly int bucketSizeInMilliseconds;
        internal readonly bool enabled;

        /*
         * This will get flipped each time a new bucket is created.
         */
        /* package for testing */
        volatile PercentileSnapshot currentPercentileSnapshot = new PercentileSnapshot(0);

        public HystrixRollingPercentile(int timeInMilliseconds, int numberOfBuckets, int bucketDataLength, bool enabled) :
            this(ACTUAL_TIME, timeInMilliseconds, numberOfBuckets, bucketDataLength, enabled)
        {

        }
        /* package for testing */
        internal HystrixRollingPercentile(ITime time, int timeInMilliseconds, int numberOfBuckets, int bucketDataLength, bool enabled)
        {
            this.time = time;
            this.timeInMilliseconds = timeInMilliseconds;
            this.numberOfBuckets = numberOfBuckets;
            this.bucketDataLength = bucketDataLength;
            this.enabled = enabled;

            if (this.timeInMilliseconds % this.numberOfBuckets != 0)
            {
                throw new ArgumentException("The timeInMilliseconds must divide equally into numberOfBuckets. For example 1000/10 is ok, 1000/11 is not.");
            }
            this.bucketSizeInMilliseconds = this.timeInMilliseconds / this.numberOfBuckets;

            buckets = new BucketCircularArray(this.numberOfBuckets);
        }

        /**
         * Add value (or values) to current bucket.
         * 
         * @param value
         *            Value to be stored in current bucket such as execution latency in milliseconds
         */
        public void AddValue(params int[] value)
        {
            /* no-op if disabled */
            if (!enabled)
                return;

            foreach (int v in value)
            {
                try
                {
                    var cbucket = GetCurrentBucket();
                    if (cbucket != null)
                        cbucket.data.AddValue(v);
                }
                catch (Exception )
                {
                    //logger.error("Failed to add value: " + v, e);
                }
            }
        }

        /**
         * Compute a percentile from the underlying rolling buckets of values.
         * <p>
         * For performance reasons it maintains a single snapshot of the sorted values from all buckets that is re-generated each time the bucket rotates.
         * <p>
         * This means that if a bucket is 5000ms, then this method will re-compute a percentile at most once every 5000ms.
         * 
         * @param percentile
         *            value such as 99 (99th percentile), 99.5 (99.5th percentile), 50 (median, 50th percentile) to compute and retrieve percentile from rolling buckets.
         * @return int percentile value
         */
        public int GetPercentile(double percentile)
        {
            /* no-op if disabled */
            if (!enabled)
                return -1;

            // force logic to move buckets forward in case other requests aren't making it happen
            GetCurrentBucket();
            // fetch the current snapshot
            return CurrentPercentileSnapshot.GetPercentile(percentile);
        }

        /**
         * This returns the mean (average) of all values in the current snapshot. This is not a percentile but often desired so captured and exposed here.
         * 
         * @return mean of all values
         */
        public int Mean
        {
            get
            {
                /* no-op if disabled */
                if (!enabled)
                    return -1;

                // force logic to move buckets forward in case other requests aren't making it happen
                GetCurrentBucket();
                // fetch the current snapshot
                return CurrentPercentileSnapshot.Mean;
            }
        }

        /**
         * This will retrieve the current snapshot or create a new one if one does not exist.
         * <p>
         * It will NOT include data from the current bucket, but all previous buckets.
         * <p>
         * It remains cached until the next bucket rotates at which point a new one will be created.
         */
        private PercentileSnapshot CurrentPercentileSnapshot
        {
            get { return currentPercentileSnapshot; }
        }

        private object newBucketLock = new object();

        private Bucket GetCurrentBucket()
        {
            long currentTime = time.CurrentTimeInMillis;

            /* a shortcut to try and get the most common result of immediately finding the current bucket */

            /**
             * Retrieve the latest bucket if the given time is BEFORE the end of the bucket window, otherwise it returns NULL.
             * 
             * NOTE: This is thread-safe because it's accessing 'buckets' which is a LinkedBlockingDeque
             */
            Bucket currentBucket = buckets.PeekLast;
            if (currentBucket != null && currentTime < currentBucket.windowStart + this.bucketSizeInMilliseconds)
            {
                // if we're within the bucket 'window of time' return the current one
                // NOTE: We do not worry if we are BEFORE the window in a weird case of where thread scheduling causes that to occur,
                // we'll just use the latest as long as we're not AFTER the window
                return currentBucket;
            }

            /* if we didn't find the current bucket above, then we have to create one */

            /**
             * The following needs to be synchronized/locked even with a synchronized/thread-safe data structure such as LinkedBlockingDeque because
             * the logic involves multiple steps to check existence, create an object then insert the object. The 'check' or 'insertion' themselves
             * are thread-safe by themselves but not the aggregate algorithm, thus we put this entire block of logic inside synchronized.
             * 
             * I am using a tryLock if/then (http://download.oracle.com/javase/6/docs/api/java/util/concurrent/locks/Lock.html#tryLock())
             * so that a single thread will get the lock and as soon as one thread gets the lock all others will go the 'else' block
             * and just return the currentBucket until the newBucket is created. This should allow the throughput to be far higher
             * and only slow down 1 thread instead of blocking all of them in each cycle of creating a new bucket based on some testing
             * (and it makes sense that it should as well).
             * 
             * This means the timing won't be exact to the millisecond as to what data ends up in a bucket, but that's acceptable.
             * It's not critical to have exact precision to the millisecond, as long as it's rolling, if we can instead reduce the impact synchronization.
             * 
             * More importantly though it means that the 'if' block within the lock needs to be careful about what it changes that can still
             * be accessed concurrently in the 'else' block since we're not completely synchronizing access.
             * 
             * For example, we can't have a multi-step process to add a bucket, remove a bucket, then update the sum since the 'else' block of code
             * can retrieve the sum while this is all happening. The trade-off is that we don't maintain the rolling sum and let readers just iterate
             * bucket to calculate the sum themselves. This is an example of favoring write-performance instead of read-performance and how the tryLock
             * versus a synchronized block needs to be accommodated.
             */

            bool lockTaken = false;
            Monitor.TryEnter(newBucketLock, ref lockTaken);
            if (lockTaken)
            {
                currentTime = time.CurrentTimeInMillis;
                try
                {
                    if (buckets.PeekLast == null)
                    {
                        // the list is empty so create the first bucket
                        Bucket newBucket = new Bucket(currentTime, bucketDataLength);
                        buckets.AddLast(newBucket);
                        return newBucket;
                    }
                    else
                    {
                        // We go into a loop so that it will create as many buckets as needed to catch up to the current time
                        // as we want the buckets complete even if we don't have transactions during a period of time.
                        for (int i = 0; i < numberOfBuckets; i++)
                        {
                            // we have at least 1 bucket so retrieve it
                            Bucket lastBucket = buckets.PeekLast;
                            if (currentTime < lastBucket.windowStart + this.bucketSizeInMilliseconds)
                            {
                                // if we're within the bucket 'window of time' return the current one
                                // NOTE: We do not worry if we are BEFORE the window in a weird case of where thread scheduling causes that to occur,
                                // we'll just use the latest as long as we're not AFTER the window
                                return lastBucket;
                            }
                            else if (currentTime - (lastBucket.windowStart + this.bucketSizeInMilliseconds) > timeInMilliseconds)
                            {
                                // the time passed is greater than the entire rolling counter so we want to clear it all and start from scratch
                                Reset();
                                // recursively call getCurrentBucket which will create a new bucket and return it
                                //return GetCurrentBucket();
                                Bucket newBucket = new Bucket(currentTime, bucketDataLength);
                                buckets.AddLast(newBucket);
                                return newBucket;
                            }
                            else
                            { // we're past the window so we need to create a new bucket
                                Bucket[] allBuckets = buckets.Array;
                                // create a new bucket and add it as the new 'last' (once this is done other threads will start using it on subsequent retrievals)
                                buckets.AddLast(new Bucket(lastBucket.windowStart + this.bucketSizeInMilliseconds, bucketDataLength));
                                // we created a new bucket so let's re-generate the PercentileSnapshot (not including the new bucket)
                                currentPercentileSnapshot = new PercentileSnapshot(allBuckets);
                            }
                        }
                        // we have finished the for-loop and created all of the buckets, so return the lastBucket now
                        return buckets.PeekLast;
                    }
                }
                finally
                {
                    Monitor.Exit(newBucketLock);
                }
            }
            else
            {
                currentBucket = buckets.PeekLast;
                if (currentBucket != null)
                {
                    // we didn't get the lock so just return the latest bucket while another thread creates the next one
                    return currentBucket;
                }
                else
                {
                    // the rare scenario where multiple threads raced to create the very first bucket
                    // wait slightly and then use recursion while the other thread finishes creating a bucket
                    if (Time.WaitUntil(() => { return buckets.PeekLast != null; }, 500))
                    {
                        return buckets.PeekLast;
                    }
                    else
                    {
                        return null;
                    }

                }
            }
        }

        /**
         * Force a reset so that percentiles start being gathered from scratch.
         */
        public void Reset()
        {
            /* no-op if disabled */
            if (!enabled)
                return;

            // clear buckets so we start over again
            buckets.Clear();

            // and also make sure the percentile snapshot gets reset
            currentPercentileSnapshot = new PercentileSnapshot(buckets.Array);
        }



        internal class PercentileBucketData
        {
            public readonly int length;
            public readonly AtomicIntegerArray list;
            public readonly AtomicInteger index = new AtomicInteger();

            public PercentileBucketData(int dataLength)
            {
                this.length = dataLength;
                this.list = new AtomicIntegerArray(dataLength);
            }

            public void AddValue(params int[] latency)
            {
                foreach (int l in latency)
                {
                    /* We just wrap around the beginning and over-write if we go past 'dataLength' as that will effectively cause us to "sample" the most recent data */
                    list[index.GetAndIncrement() % length] = l;
                    // TODO Alternative to AtomicInteger? The getAndIncrement may be a source of contention on high throughput circuits on large multi-core systems.
                    // LongAdder isn't suited to this as it is not consistent. Perhaps a different data structure that doesn't need indexed adds?
                    // A threadlocal data storage that only aggregates when fetched would be ideal. Similar to LongAdder except for accumulating lists of data.
                }
            }

            public int Length
            {
                get
                {
                    if (index.Value > list.Length)
                    {
                        return list.Length;
                    }
                    else
                    {
                        return index.Value;
                    }
                }
            }

        }

        internal class PercentileSnapshot
        {
            private readonly int[] data;
            private readonly int length;
            private int mean;

            /* package for testing */
            public PercentileSnapshot(Bucket[] buckets)
            {
                int lengthFromBuckets = 0;
                // we need to calculate it dynamically as it could have been changed by properties (rare, but possible)
                // also this way we capture the actual index size rather than the max so size the int[] to only what we need
                foreach (Bucket bd in buckets)
                {
                    lengthFromBuckets += bd.data.length;
                }
                data = new int[lengthFromBuckets];
                int index = 0;
                int sum = 0;
                foreach (Bucket bd in buckets)
                {
                    PercentileBucketData pbd = bd.data;
                    int length = pbd.Length;
                    for (int i = 0; i < length; i++)
                    {
                        int v = pbd.list[i];
                        this.data[index++] = v;
                        sum += v;
                    }
                }
                this.length = index;
                if (this.length == 0)
                {
                    this.mean = 0;
                }
                else
                {
                    this.mean = sum / this.length;
                }

                Array.Sort(this.data, 0, length);
            }

            /* package for testing */
            public PercentileSnapshot(params int[] data)
            {
                this.data = data;
                this.length = data.Length;

                int sum = 0;
                foreach (int v in data)
                {
                    sum += v;
                }
                this.mean = sum / this.length;

                Array.Sort(this.data, 0, length);
            }

            /* package for testing */
            public int Mean
            {
                get { return mean; }
            }

            /**
             * Provides percentile computation.
             */
            public int GetPercentile(double percentile)
            {
                if (length == 0)
                {
                    return 0;
                }
                return ComputePercentile(percentile);
            }

            /**
             * @see <a href="http://en.wikipedia.org/wiki/Percentile">Percentile (Wikipedia)</a>
             * @see <a href="http://cnx.org/content/m10805/latest/">Percentile</a>
             * 
             * @param percent percentile of data desired
             * @return data at the asked-for percentile.  Interpolation is used if exactness is not possible
             */
            private int ComputePercentile(double percent)
            {
                // Some just-in-case edge cases
                if (length <= 0)
                {
                    return 0;
                }
                else if (percent <= 0.0)
                {
                    return data[0];
                }
                else if (percent >= 100.0)
                {
                    return data[length - 1];
                }

                // ranking (http://en.wikipedia.org/wiki/Percentile#Alternative_methods)
                double rank = (percent / 100.0) * length;

                // linear interpolation between closest ranks
                int iLow = (int)Math.Floor(rank);
                int iHigh = (int)Math.Ceiling(rank);
                //assert 0 <= iLow && iLow <= rank && rank <= iHigh && iHigh <= length;
                // assert(iHigh - iLow) <= 1;
                if (iHigh >= length)
                {
                    // Another edge case
                    return data[length - 1];
                }
                else if (iLow == iHigh)
                {
                    return data[iLow];
                }
                else
                {
                    // Interpolate between the two bounding values
                    return (int)(data[iLow] + (rank - iLow) * (data[iHigh] - data[iLow]));
                }
            }

        }

        internal class BucketCircularArray : IEnumerable<Bucket>
        {

            private readonly AtomicReference<ListState> state;
            private readonly int dataLength; // we don't resize, we always stay the same, so remember this
            private readonly int numBuckets;

            /**
             * Immutable object that is atomically set every time the state of the BucketCircularArray changes
             * <p>
             * This handles the compound operations
             */
            class ListState
            {
                /*
                 * this is an AtomicReferenceArray and not a normal Array because we're copying the reference
                 * between ListState objects and multiple threads could maintain references across these
                 * compound operations so I want the visibility/concurrency guarantees
                 */
                public readonly AtomicReferenceArray<Bucket> data;
                public readonly int size;
                public readonly int tail;
                public readonly int head;
                public BucketCircularArray cb;

                public ListState(BucketCircularArray cb, AtomicReferenceArray<Bucket> data, int head, int tail)
                {
                    this.cb = cb;
                    this.head = head;
                    this.tail = tail;
                    if (head == 0 && tail == 0)
                    {
                        size = 0;
                    }
                    else
                    {
                        this.size = (tail + cb.dataLength - head) % cb.dataLength;
                    }
                    this.data = data;
                }

                public Bucket Tail
                {
                    get
                    {
                        if (size == 0)
                        {
                            return null;
                        }
                        else
                        {
                            // we want to get the last item, so size()-1
                            return data[(Convert(size - 1))];
                        }
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
                        List<Bucket> array = new List<Bucket>();
                        for (int i = 0; i < size; i++)
                        {
                            array.Add(data[(Convert(i))]);
                        }
                        return array.ToArray();
                    }
                }

                private ListState IncrementTail()
                {
                    /* if incrementing results in growing larger than 'length' which is the max we should be at, then also increment head (equivalent of removeFirst but done atomically) */
                    if (size == cb.numBuckets)
                    {
                        // increment tail and head
                        return new ListState(cb, data, (head + 1) % cb.dataLength, (tail + 1) % cb.dataLength);
                    }
                    else
                    {
                        // increment only tail
                        return new ListState(cb, data, head, (tail + 1) % cb.dataLength);
                    }
                }

                public ListState Clear()
                {
                    return new ListState(cb, new AtomicReferenceArray<Bucket>(cb.dataLength), 0, 0);
                }

                public ListState AddBucket(Bucket b)
                {
                    /*
                     * We could in theory have 2 threads addBucket concurrently and this compound operation would interleave.
                     * <p>
                     * This should NOT happen since getCurrentBucket is supposed to be executed by a single thread.
                     * <p>
                     * If it does happen, it's not a huge deal as incrementTail() will be protected by compareAndSet and one of the two addBucket calls will succeed with one of the Buckets.
                     * <p>
                     * In either case, a single Bucket will be returned as "last" and data loss should not occur and everything keeps in sync for head/tail.
                     * <p>
                     * Also, it's fine to set it before incrementTail because nothing else should be referencing that index position until incrementTail occurs.
                     */
                    data[tail] = b;
                    return IncrementTail();
                }

                // The convert() method takes a logical index (as if head was
                // always 0) and calculates the index within elementData
                private int Convert(int index)
                {
                    return (index + head) % cb.dataLength;
                }
            }

            public BucketCircularArray(int size)
            {
                AtomicReferenceArray<Bucket> _buckets = new AtomicReferenceArray<Bucket>(size + 1); // + 1 as extra room for the add/remove;
                state = new AtomicReference<ListState>(new ListState(this, _buckets, 0, 0));
                dataLength = _buckets.Length;
                numBuckets = size;
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
                     * 
                     * Instead of trying to determine if someone already successfully called clear() and we should skip
                     * we will have both calls reset the circuit, even if that means losing data added in between the two
                     * depending on thread scheduling.
                     * 
                     * The rare scenario in which that would occur, we'll accept the possible data loss while clearing it
                     * since the code has stated its desire to clear() anyways.
                     */
                    ListState current = state.Value;
                    ListState newState = current.Clear();
                    if (state.CompareAndSet(current, newState))
                    {
                        return;
                    }
                }
            }


            public void AddLast(Bucket o)
            {
                ListState currentState = state.Value;
                // create new version of state (what we want it to become)
                ListState newState = currentState.AddBucket(o);

                /*
                 * use compareAndSet to set in case multiple threads are attempting (which shouldn't be the case because since addLast will ONLY be called by a single thread at a time due to protection
                 * provided in <code>getCurrentBucket</code>)
                 */
                if (state.CompareAndSet(currentState, newState))
                {
                    // we succeeded
                    return;
                }
                else
                {
                    // we failed, someone else was adding or removing
                    // instead of trying again and risking multiple addLast concurrently (which shouldn't be the case)
                    // we'll just return and let the other thread 'win' and if the timing is off the next call to getCurrentBucket will fix things
                    return;
                }
            }

            public int Size
            {
                // the size can also be worked out each time as:
                // return (tail + data.length() - head) % data.length();
                get { return state.Value.size; }
            }

            public Bucket PeekLast
            {
                get { return state.Value.Tail; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<Bucket> GetEnumerator()
            {
                List<Bucket> list = new List<Bucket>(Array);
                return list.AsReadOnly().GetEnumerator();
            }

            public Bucket[] Array
            {
                get { return state.Value.Array; }
            }

        }

        internal class Bucket
        {
            public readonly long windowStart;
            public readonly PercentileBucketData data;

            public Bucket(long startTime, int bucketDataLength)
            {
                this.windowStart = startTime;
                this.data = new PercentileBucketData(bucketDataLength);
            }

        }
    }

}
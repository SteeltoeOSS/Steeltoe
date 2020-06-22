﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class HystrixRollingPercentile
    {
        internal readonly BucketCircularArray _buckets;
        internal readonly int _timeInMilliseconds;
        internal readonly int _numberOfBuckets;
        internal readonly int _bucketDataLength;
        internal readonly int _bucketSizeInMilliseconds;
        internal readonly bool _enabled;

        private static ITime actual_time = new ActualTime();
        private readonly ITime time;

        /*
         * This will get flipped each time a new bucket is created.
         */
        /* package for testing */
        private volatile PercentileSnapshot currentPercentileSnapshot = new PercentileSnapshot(0);

        public HystrixRollingPercentile(int timeInMilliseconds, int numberOfBuckets, int bucketDataLength, bool enabled)
            : this(actual_time, timeInMilliseconds, numberOfBuckets, bucketDataLength, enabled)
        {
        }

        /* package for testing */
        internal HystrixRollingPercentile(ITime time, int timeInMilliseconds, int numberOfBuckets, int bucketDataLength, bool enabled)
        {
            this.time = time;
            _timeInMilliseconds = timeInMilliseconds;
            _numberOfBuckets = numberOfBuckets;
            _bucketDataLength = bucketDataLength;
            _enabled = enabled;

            if (_timeInMilliseconds % _numberOfBuckets != 0)
            {
                throw new ArgumentException("The timeInMilliseconds must divide equally into numberOfBuckets. For example 1000/10 is ok, 1000/11 is not.");
            }

            _bucketSizeInMilliseconds = _timeInMilliseconds / _numberOfBuckets;

            _buckets = new BucketCircularArray(_numberOfBuckets);
        }

        public void AddValue(params int[] value)
        {
            /* no-op if disabled */
            if (!_enabled)
            {
                return;
            }

            foreach (int v in value)
            {
                try
                {
                    var cbucket = GetCurrentBucket();
                    if (cbucket != null)
                    {
                        cbucket._data.AddValue(v);
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
            if (!_enabled)
            {
                return -1;
            }

            // force logic to move buckets forward in case other requests aren't making it happen
            GetCurrentBucket();

            // fetch the current snapshot
            return CurrentPercentileSnapshot.GetPercentile(percentile);
        }

        public int Mean
        {
            get
            {
                /* no-op if disabled */
                if (!_enabled)
                {
                    return -1;
                }

                // force logic to move buckets forward in case other requests aren't making it happen
                GetCurrentBucket();

                // fetch the current snapshot
                return CurrentPercentileSnapshot.Mean;
            }
        }

        public void Reset()
        {
            /* no-op if disabled */
            if (!_enabled)
            {
                return;
            }

            // clear buckets so we start over again
            _buckets.Clear();

            // and also make sure the percentile snapshot gets reset
            currentPercentileSnapshot = new PercentileSnapshot(_buckets.Array);
        }

        private PercentileSnapshot CurrentPercentileSnapshot => currentPercentileSnapshot;

        private object newBucketLock = new object();

        private Bucket GetCurrentBucket()
        {
            long currentTime = time.CurrentTimeInMillis;

            /* a shortcut to try and get the most common result of immediately finding the current bucket */

            /*
             * Retrieve the latest bucket if the given time is BEFORE the end of the bucket window, otherwise it returns NULL.
             * NOTE: This is thread-safe because it's accessing 'buckets' which is a LinkedBlockingDeque
             */
            Bucket currentBucket = _buckets.PeekLast;
            if (currentBucket != null && currentTime < currentBucket._windowStart + _bucketSizeInMilliseconds)
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
            Monitor.TryEnter(newBucketLock, ref lockTaken);
            if (lockTaken)
            {
                currentTime = time.CurrentTimeInMillis;
                try
                {
                    if (_buckets.PeekLast == null)
                    {
                        // the list is empty so create the first bucket
                        Bucket newBucket = new Bucket(currentTime, _bucketDataLength);
                        _buckets.AddLast(newBucket);
                        return newBucket;
                    }
                    else
                    {
                        // We go into a loop so that it will create as many buckets as needed to catch up to the current time
                        // as we want the buckets complete even if we don't have transactions during a period of time.
                        for (int i = 0; i < _numberOfBuckets; i++)
                        {
                            // we have at least 1 bucket so retrieve it
                            Bucket lastBucket = _buckets.PeekLast;
                            if (currentTime < lastBucket._windowStart + _bucketSizeInMilliseconds)
                            {
                                // if we're within the bucket 'window of time' return the current one
                                // NOTE: We do not worry if we are BEFORE the window in a weird case of where thread scheduling causes that to occur,
                                // we'll just use the latest as long as we're not AFTER the window
                                return lastBucket;
                            }
                            else if (currentTime - (lastBucket._windowStart + _bucketSizeInMilliseconds) > _timeInMilliseconds)
                            {
                                // the time passed is greater than the entire rolling counter so we want to clear it all and start from scratch
                                Reset();

                                // recursively call getCurrentBucket which will create a new bucket and return it
                                // return GetCurrentBucket();
                                Bucket newBucket = new Bucket(currentTime, _bucketDataLength);
                                _buckets.AddLast(newBucket);
                                return newBucket;
                            }
                            else
                            {
                                // we're past the window so we need to create a new bucket
                                Bucket[] allBuckets = _buckets.Array;

                                // create a new bucket and add it as the new 'last' (once this is done other threads will start using it on subsequent retrievals)
                                _buckets.AddLast(new Bucket(lastBucket._windowStart + _bucketSizeInMilliseconds, _bucketDataLength));

                                // we created a new bucket so let's re-generate the PercentileSnapshot (not including the new bucket)
                                currentPercentileSnapshot = new PercentileSnapshot(allBuckets);
                            }
                        }

                        // we have finished the for-loop and created all of the buckets, so return the lastBucket now
                        return _buckets.PeekLast;
                    }
                }
                finally
                {
                    Monitor.Exit(newBucketLock);
                }
            }
            else
            {
                currentBucket = _buckets.PeekLast;
                if (currentBucket != null)
                {
                    // we didn't get the lock so just return the latest bucket while another thread creates the next one
                    return currentBucket;
                }
                else
                {
                    // the rare scenario where multiple threads raced to create the very first bucket
                    // wait slightly and then use recursion while the other thread finishes creating a bucket
                    if (Time.WaitUntil(() => { return _buckets.PeekLast != null; }, 500))
                    {
                        return _buckets.PeekLast;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        internal class PercentileBucketData
        {
            internal readonly int _datalength;
            internal readonly AtomicIntegerArray _list;
            internal readonly AtomicInteger _index = new AtomicInteger();

            public PercentileBucketData(int dataLength)
            {
                _datalength = dataLength;
                _list = new AtomicIntegerArray(dataLength);
            }

            public void AddValue(params int[] latency)
            {
                foreach (int l in latency)
                {
                    /* We just wrap around the beginning and over-write if we go past 'dataLength' as that will effectively cause us to "sample" the most recent data */
                    _list[_index.GetAndIncrement() % _datalength] = l;

                    // TODO Alternative to AtomicInteger? The getAndIncrement may be a source of contention on high throughput circuits on large multi-core systems.
                    // LongAdder isn't suited to this as it is not consistent. Perhaps a different data structure that doesn't need indexed adds?
                    // A threadlocal data storage that only aggregates when fetched would be ideal. Similar to LongAdder except for accumulating lists of data.
                }
            }

            public int Length
            {
                get
                {
                    if (_index.Value > _list.Length)
                    {
                        return _list.Length;
                    }
                    else
                    {
                        return _index.Value;
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
                    lengthFromBuckets += bd._data._datalength;
                }

                data = new int[lengthFromBuckets];
                int index = 0;
                int sum = 0;
                foreach (Bucket bd in buckets)
                {
                    PercentileBucketData pbd = bd._data;
                    int pbdLength = pbd.Length;
                    for (int i = 0; i < pbdLength; i++)
                    {
                        int v = pbd._list[i];
                        data[index++] = v;
                        sum += v;
                    }
                }

                length = index;
                if (length == 0)
                {
                    mean = 0;
                }
                else
                {
                    mean = sum / length;
                }

                Array.Sort(data, 0, length);
            }

            /* package for testing */
            public PercentileSnapshot(params int[] data)
            {
                this.data = data;
                length = data.Length;

                int sum = 0;
                foreach (int v in data)
                {
                    sum += v;
                }

                mean = sum / length;

                Array.Sort(this.data, 0, length);
            }

            /* package for testing */
            public int Mean => mean;

            public int GetPercentile(double percentile)
            {
                if (length == 0)
                {
                    return 0;
                }

                return ComputePercentile(percentile);
            }

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

                // ranking (https://en.wikipedia.org/wiki/Percentile#Alternative_methods)
                double rank = (percent / 100.0) * length;

                // linear interpolation between closest ranks
                int iLow = (int)Math.Floor(rank);
                int iHigh = (int)Math.Ceiling(rank);

                // assert 0 <= iLow && iLow <= rank && rank <= iHigh && iHigh <= length;
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
                    return (int)(data[iLow] + ((rank - iLow) * (data[iHigh] - data[iLow])));
                }
            }
        }

        internal class BucketCircularArray : IEnumerable<Bucket>
        {
            private readonly AtomicReference<ListState> state;
            private readonly int dataLength; // we don't resize, we always stay the same, so remember this
            private readonly int numBuckets;

            internal class ListState
            {
                /*
                 * this is an AtomicReferenceArray and not a normal Array because we're copying the reference
                 * between ListState objects and multiple threads could maintain references across these
                 * compound operations so I want the visibility/concurrency guarantees
                 */
                internal readonly AtomicReferenceArray<Bucket> _data;
                internal readonly int _size;
                internal readonly int _buckettail;
                internal readonly int _head;
                internal BucketCircularArray _cb;

                public ListState(BucketCircularArray cb, AtomicReferenceArray<Bucket> data, int head, int tail)
                {
                    _cb = cb;
                    _head = head;
                    _buckettail = tail;
                    if (head == 0 && tail == 0)
                    {
                        _size = 0;
                    }
                    else
                    {
                        _size = (tail + cb.dataLength - head) % cb.dataLength;
                    }

                    _data = data;
                }

                public Bucket Tail
                {
                    get
                    {
                        if (_size == 0)
                        {
                            return null;
                        }
                        else
                        {
                            // we want to get the last item, so size()-1
                            return _data[Convert(_size - 1)];
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
                        for (int i = 0; i < _size; i++)
                        {
                            array.Add(_data[Convert(i)]);
                        }

                        return array.ToArray();
                    }
                }

                public ListState Clear()
                {
                    return new ListState(_cb, new AtomicReferenceArray<Bucket>(_cb.dataLength), 0, 0);
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
                    _data[_buckettail] = b;
                    return IncrementTail();
                }

                // The convert() method takes a logical index (as if head was
                // always 0) and calculates the index within elementData
                private int Convert(int index)
                {
                    return (index + _head) % _cb.dataLength;
                }

                private ListState IncrementTail()
                {
                    /* if incrementing results in growing larger than 'length' which is the max we should be at, then also increment head (equivalent of removeFirst but done atomically) */
                    if (_size == _cb.numBuckets)
                    {
                        // increment tail and head
                        return new ListState(_cb, _data, (_head + 1) % _cb.dataLength, (_buckettail + 1) % _cb.dataLength);
                    }
                    else
                    {
                        // increment only tail
                        return new ListState(_cb, _data, _head, (_buckettail + 1) % _cb.dataLength);
                    }
                }
            }

            public BucketCircularArray(int size)
            {
                AtomicReferenceArray<Bucket> buckets = new AtomicReferenceArray<Bucket>(size + 1); // + 1 as extra room for the add/remove;
                state = new AtomicReference<ListState>(new ListState(this, buckets, 0, 0));
                dataLength = buckets.Length;
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
                     * Instead of trying to determine if someone already successfully called clear() and we should skip
                     * we will have both calls reset the circuit, even if that means losing data added in between the two
                     * depending on thread scheduling.
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
#pragma warning disable S3923 // All branches in a conditional structure should not have exactly the same implementation
                if (state.CompareAndSet(currentState, newState))
                {
                    // we succeeded
                }
                else
                {
                    // we failed, someone else was adding or removing
                    // instead of trying again and risking multiple addLast concurrently (which shouldn't be the case)
                    // we'll just return and let the other thread 'win' and if the timing is off the next call to getCurrentBucket will fix things
                }
#pragma warning restore S3923 // All branches in a conditional structure should not have exactly the same implementation
            }

            public int Size
            {
                // the size can also be worked out each time as:
                // return (tail + data.length() - head) % data.length();
                get { return state.Value._size; }
            }

            public Bucket PeekLast => state.Value.Tail;

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<Bucket> GetEnumerator()
            {
                List<Bucket> list = new List<Bucket>(Array);
                return list.AsReadOnly().GetEnumerator();
            }

            public Bucket[] Array => state.Value.Array;
        }

        internal class Bucket
        {
            internal readonly long _windowStart;
            internal readonly PercentileBucketData _data;

            public Bucket(long startTime, int bucketDataLength)
            {
                _windowStart = startTime;
                _data = new PercentileBucketData(bucketDataLength);
            }
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class HystrixRollingNumber
    {
        internal readonly int _timeInMilliseconds;
        internal readonly int _numberOfBuckets;
        internal readonly int _bucketSizeInMillseconds;

        internal readonly BucketCircularArray _buckets;
        internal readonly CumulativeSum _cumulativeSum = new CumulativeSum();

        private static ITime actual_time = new ActualTime();
        private readonly ITime _time;

        public HystrixRollingNumber(int timeInMilliseconds, int numberOfBuckets)
            : this(actual_time, timeInMilliseconds, numberOfBuckets)
        {
        }

        /* package for testing */
        internal HystrixRollingNumber(ITime time, int timeInMilliseconds, int numberOfBuckets)
        {
            _time = time;
            _timeInMilliseconds = timeInMilliseconds;
            _numberOfBuckets = numberOfBuckets;

            if (timeInMilliseconds % numberOfBuckets != 0)
            {
                throw new ArgumentException("The timeInMilliseconds must divide equally into numberOfBuckets. For example 1000/10 is ok, 1000/11 is not.");
            }

            _bucketSizeInMillseconds = timeInMilliseconds / numberOfBuckets;

            _buckets = new BucketCircularArray(numberOfBuckets);
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
            var lastBucket = _buckets.PeekLast;
            if (lastBucket != null)
            {
                _cumulativeSum.AddBucket(lastBucket);
            }

            // clear buckets so we start over again
            _buckets.Clear();
        }

        public long GetCumulativeSum(HystrixRollingNumberEvent type)
        {
            // this isn't 100% atomic since multiple threads can be affecting latestBucket & cumulativeSum independently
            // but that's okay since the count is always a moving target and we're accepting a "point in time" best attempt
            // we are however putting 'getValueOfLatestBucket' first since it can have side-affects on cumulativeSum whereas the inverse is not true
            return GetValueOfLatestBucket(type) + _cumulativeSum.Get(type);
        }

        public long GetRollingSum(HystrixRollingNumberEvent type)
        {
            var lastBucket = GetCurrentBucket();
            if (lastBucket == null)
            {
                return 0;
            }

            long sum = 0;
            foreach (var b in _buckets)
            {
                sum += b.GetAdder(type).Sum();
            }

            return sum;
        }

        public long GetValueOfLatestBucket(HystrixRollingNumberEvent type)
        {
            var lastBucket = GetCurrentBucket();
            if (lastBucket == null)
            {
                return 0;
            }

            // we have bucket data so we'll return the lastBucket
            return lastBucket.Get(type);
        }

        public long[] GetValues(HystrixRollingNumberEvent type)
        {
            var lastBucket = GetCurrentBucket();
            if (lastBucket == null)
            {
                return Array.Empty<long>();
            }

            // get buckets as an array (which is a copy of the current state at this point in time)
            var bucketArray = _buckets.Array;

            // we have bucket data so we'll return an array of values for all buckets
            var values = new long[bucketArray.Length];
            var i = 0;
            foreach (var bucket in bucketArray)
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
            var values = GetValues(type);
            if (values.Length == 0)
            {
                return 0;
            }
            else
            {
                Array.Sort(values);
                return values[values.Length - 1];
            }
        }

        private object _newBucketLock = new object();

        /* package for testing */
        internal Bucket GetCurrentBucket()
        {
            var currentTime = _time.CurrentTimeInMillis;

            /* a shortcut to try and get the most common result of immediately finding the current bucket */

            /*
             * Retrieve the latest bucket if the given time is BEFORE the end of the bucket window, otherwise it returns NULL.
             * NOTE: This is thread-safe because it's accessing 'buckets' which is a LinkedBlockingDeque
             */
            var currentBucket = _buckets.PeekLast;
            if (currentBucket != null && currentTime < currentBucket._windowStart + _bucketSizeInMillseconds)
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
            var lockTaken = false;
            Monitor.TryEnter(_newBucketLock, ref lockTaken);
            if (lockTaken)
            {
                currentTime = _time.CurrentTimeInMillis;
                try
                {
                    if (_buckets.PeekLast == null)
                    {
                        // the list is empty so create the first bucket
                        var newBucket = new Bucket(currentTime);
                        _buckets.AddLast(newBucket);
                        return newBucket;
                    }
                    else
                    {
                        // We go into a loop so that it will create as many buckets as needed to catch up to the current time
                        // as we want the buckets complete even if we don't have transactions during a period of time.
                        for (var i = 0; i < _numberOfBuckets; i++)
                        {
                            // we have at least 1 bucket so retrieve it
                            var lastBucket = _buckets.PeekLast;
                            if (currentTime < lastBucket._windowStart + _bucketSizeInMillseconds)
                            {
                                // if we're within the bucket 'window of time' return the current one
                                // NOTE: We do not worry if we are BEFORE the window in a weird case of where thread scheduling causes that to occur,
                                // we'll just use the latest as long as we're not AFTER the window
                                return lastBucket;
                            }
                            else if (currentTime - (lastBucket._windowStart + _bucketSizeInMillseconds) > _timeInMilliseconds)
                            {
                                // the time passed is greater than the entire rolling counter so we want to clear it all and start from scratch
                                Reset();

                                var newBucket = new Bucket(currentTime);
                                _buckets.AddLast(newBucket);
                                return newBucket;
                            }
                            else
                            {
                                // we're past the window so we need to create a new bucket
                                // create a new bucket and add it as the new 'last'
                                _buckets.AddLast(new Bucket(lastBucket._windowStart + _bucketSizeInMillseconds));

                                // add the lastBucket values to the cumulativeSum
                                _cumulativeSum.AddBucket(lastBucket);
                            }
                        }

                        // we have finished the for-loop and created all of the buckets, so return the lastBucket now
                        return _buckets.PeekLast;
                    }
                }
                finally
                {
                    Monitor.Exit(_newBucketLock);
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

        internal class Bucket
        {
            internal readonly long _windowStart;
            internal readonly LongAdder[] _adderForCounterType;
            internal readonly LongMaxUpdater[] _updaterForCounterType;

            public Bucket(long startTime)
            {
                _windowStart = startTime;

                /*
                 * We support both LongAdder and LongMaxUpdater in a bucket but don't want the memory allocation
                 * of all types for each so we only allocate the objects if the HystrixRollingNumberEvent matches
                 * the correct type - though we still have the allocation of empty arrays to the given length
                 * as we want to keep using the type.ordinal() value for fast random access.
                 */

                // initialize the array of LongAdders
                _adderForCounterType = new LongAdder[HystrixRollingNumberEventHelper.Values.Count];
                foreach (var type in HystrixRollingNumberEventHelper.Values)
                {
                    if (HystrixRollingNumberEventHelper.IsCounter(type))
                    {
                        _adderForCounterType[(int)type] = new LongAdder();
                    }
                }

                _updaterForCounterType = new LongMaxUpdater[HystrixRollingNumberEventHelper.Values.Count];
                foreach (var type in HystrixRollingNumberEventHelper.Values)
                {
                    if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                    {
                        _updaterForCounterType[(int)type] = new LongMaxUpdater();

                        // initialize to 0 otherwise it is Long.MIN_VALUE
                        _updaterForCounterType[(int)type].Update(0);
                    }
                }
            }

            public long Get(HystrixRollingNumberEvent type)
            {
                if (HystrixRollingNumberEventHelper.IsCounter(type))
                {
                    return _adderForCounterType[(int)type].Sum();
                }

                if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                {
                    return _updaterForCounterType[(int)type].Max;
                }

                throw new InvalidOperationException("Unknown type of event: " + type.ToString());
            }

            public LongAdder GetAdder(HystrixRollingNumberEvent type)
            {
                if (!HystrixRollingNumberEventHelper.IsCounter(type))
                {
                    throw new InvalidOperationException("Type is not a Counter: " + type.ToString());
                }

                return _adderForCounterType[(int)type];
            }

            public LongMaxUpdater GetMaxUpdater(HystrixRollingNumberEvent type)
            {
                if (!HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                {
                    throw new InvalidOperationException("Type is not a MaxUpdater: " + type.ToString());
                }

                return _updaterForCounterType[(int)type];
            }
        }

        internal class CumulativeSum
        {
            internal readonly LongAdder[] _adderForCounterType;
            internal readonly LongMaxUpdater[] _updaterForCounterType;

            public CumulativeSum()
            {
                /*
                 * We support both LongAdder and LongMaxUpdater in a bucket but don't want the memory allocation
                 * of all types for each so we only allocate the objects if the HystrixRollingNumberEvent matches
                 * the correct type - though we still have the allocation of empty arrays to the given length
                 * as we want to keep using the type.ordinal() value for fast random access.
                 */

                // initialize the array of LongAdders
                _adderForCounterType = new LongAdder[HystrixRollingNumberEventHelper.Values.Count];
                foreach (var type in HystrixRollingNumberEventHelper.Values)
                {
                    if (HystrixRollingNumberEventHelper.IsCounter(type))
                    {
                        _adderForCounterType[(int)type] = new LongAdder();
                    }
                }

                _updaterForCounterType = new LongMaxUpdater[HystrixRollingNumberEventHelper.Values.Count];
                foreach (var type in HystrixRollingNumberEventHelper.Values)
                {
                    if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                    {
                        _updaterForCounterType[(int)type] = new LongMaxUpdater();

                        // initialize to 0 otherwise it is Long.MIN_VALUE
                        _updaterForCounterType[(int)type].Update(0);
                    }
                }
            }

            public void AddBucket(Bucket lastBucket)
            {
                foreach (var type in HystrixRollingNumberEventHelper.Values)
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
                    return _adderForCounterType[(int)type].Sum();
                }

                if (HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                {
                    return _updaterForCounterType[(int)type].Max;
                }

                throw new InvalidOperationException("Unknown type of event: " + type.ToString());
            }

            public LongAdder GetAdder(HystrixRollingNumberEvent type)
            {
                if (!HystrixRollingNumberEventHelper.IsCounter(type))
                {
                    throw new InvalidOperationException("Type is not a Counter: " + type.ToString());
                }

                return _adderForCounterType[(int)type];
            }

            public LongMaxUpdater GetMaxUpdater(HystrixRollingNumberEvent type)
            {
                if (!HystrixRollingNumberEventHelper.IsMaxUpdater(type))
                {
                    throw new InvalidOperationException("Type is not a MaxUpdater: " + type.ToString());
                }

                return _updaterForCounterType[(int)type];
            }
        }

        internal class BucketCircularArray : IEnumerable<Bucket>
        {
            private readonly AtomicReference<ListState> _state;
            private readonly int _dataLength; // we don't resize, we always stay the same, so remember this
            private readonly int _numBuckets;

            internal class ListState
            {
                /*
                    * this is an AtomicReferenceArray and not a normal Array because we're copying the reference
                    * between ListState objects and multiple threads could maintain references across these
                    * compound operations so I want the visibility/concurrency guarantees
                    */
                internal readonly AtomicReferenceArray<Bucket> _data;
                internal readonly int _size;
                internal readonly int _listtail;
                internal readonly int _head;
                internal readonly BucketCircularArray _ca;

                public ListState(BucketCircularArray ca, AtomicReferenceArray<Bucket> data, int head, int tail)
                {
                    _ca = ca;
                    _head = head;
                    _listtail = tail;
                    if (head == 0 && tail == 0)
                    {
                        _size = 0;
                    }
                    else
                    {
                        _size = (tail + ca._dataLength - head) % ca._dataLength;
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
                        var array = new List<Bucket>();
                        for (var i = 0; i < _size; i++)
                        {
                            array.Add(_data[Convert(i)]);
                        }

                        return array.ToArray();
                    }
                }

                public ListState Clear()
                {
                    return new ListState(_ca, new AtomicReferenceArray<Bucket>(_ca._dataLength), 0, 0);
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
                    _data[_listtail] = b;
                    return IncrementTail();
                }

                // The convert() method takes a logical index (as if head was
                // always 0) and calculates the index within elementData
                private int Convert(int index)
                {
                    return (index + _head) % _ca._dataLength;
                }

                private ListState IncrementTail()
                {
                    /* if incrementing results in growing larger than 'length' which is the max we should be at, then also increment head (equivalent of removeFirst but done atomically) */
                    if (_size == _ca._numBuckets)
                    {
                        // increment tail and head
                        return new ListState(_ca, _data, (_head + 1) % _ca._dataLength, (_listtail + 1) % _ca._dataLength);
                    }
                    else
                    {
                        // increment only tail
                        return new ListState(_ca, _data, _head, (_listtail + 1) % _ca._dataLength);
                    }
                }
            }

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
                    var current = _state.Value;
                    var newState = current.Clear();
                    if (_state.CompareAndSet(current, newState))
                    {
                        return;
                    }
                }
            }

            public void AddLast(Bucket o)
            {
                var currentState = _state.Value;

                // create new version of state (what we want it to become)
                var newState = currentState.AddBucket(o);

                /*
                 * use compareAndSet to set in case multiple threads are attempting (which shouldn't be the case because since addLast will ONLY be called by a single thread at a time due to protection
                 * provided in <code>getCurrentBucket</code>)
                 */
#pragma warning disable S3923 // All branches in a conditional structure should not have exactly the same implementation
                if (_state.CompareAndSet(currentState, newState))
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

            public Bucket Last
            {
                get { return PeekLast; }
            }

            public int Size
            {
                // the size can also be worked out each time as:
                // return (tail + data.length() - head) % data.length();
                get { return _state.Value._size; }
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

            public Bucket PeekLast
            {
                get { return _state.Value.Tail; }
            }

            public Bucket[] Array
            {
                get { return _state.Value.Array; }
            }
        }
    }
}
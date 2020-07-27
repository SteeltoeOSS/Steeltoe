// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test
{
    public class HystrixRollingNumberTest
    {
        private ITestOutputHelper output;

        public HystrixRollingNumberTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestCreatesBuckets()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // confirm the initial settings
                Assert.Equal(200, counter._timeInMilliseconds);
                Assert.Equal(10, counter._numberOfBuckets);
                Assert.Equal(20, counter._bucketSizeInMillseconds);

                // we start out with 0 buckets in the queue
                Assert.Equal(0, counter._buckets.Size);

                // add a success in each interval which should result in all 10 buckets being created with 1 success in each
                for (var i = 0; i < counter._numberOfBuckets; i++)
                {
                    counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                    time.Increment(counter._bucketSizeInMillseconds);
                }

                // confirm we have all 10 buckets
                Assert.Equal(10, counter._buckets.Size);

                // add 1 more and we should still only have 10 buckets since that's the max
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                Assert.Equal(10, counter._buckets.Size);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestResetBuckets()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // we start out with 0 buckets in the queue
                Assert.Equal(0, counter._buckets.Size);

                // add 1
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);

                // confirm we have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // confirm we still have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // add 1
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);

                // we should now have a single bucket with no values in it instead of 2 or more buckets
                Assert.Equal(1, counter._buckets.Size);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestEmptyBucketsFillIn()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // add 1
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);

                // we should have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // wait past 3 bucket time periods (the 1st bucket then 2 empty ones)
                time.Increment(counter._bucketSizeInMillseconds * 3);

                // add another
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);

                // we should have 4 (1 + 2 empty + 1 new one) buckets
                Assert.Equal(4, counter._buckets.Size);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestIncrementInSingleBucket()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.TIMEOUT);

                // we should have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // the count should be 4
                Assert.Equal(4, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.SUCCESS).Sum());
                Assert.Equal(2, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.FAILURE).Sum());
                Assert.Equal(1, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.TIMEOUT).Sum());
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestTimeout()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.Increment(HystrixRollingNumberEvent.TIMEOUT);

                // we should have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // the count should be 1
                Assert.Equal(1, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.TIMEOUT).Sum());
                Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.TIMEOUT));

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds * 3);

                // incremenet again in latest bucket
                counter.Increment(HystrixRollingNumberEvent.TIMEOUT);

                // we should have 4 buckets
                Assert.Equal(4, counter._buckets.Size);

                // the counts of the last bucket
                Assert.Equal(1, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.TIMEOUT).Sum());

                // the total counts
                Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.TIMEOUT));
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestShortCircuited()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.Increment(HystrixRollingNumberEvent.SHORT_CIRCUITED);

                // we should have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // the count should be 1
                Assert.Equal(1, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.SHORT_CIRCUITED).Sum());
                Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.SHORT_CIRCUITED));

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds * 3);

                // incremenet again in latest bucket
                counter.Increment(HystrixRollingNumberEvent.SHORT_CIRCUITED);

                // we should have 4 buckets
                Assert.Equal(4, counter._buckets.Size);

                // the counts of the last bucket
                Assert.Equal(1, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.SHORT_CIRCUITED).Sum());

                // the total counts
                Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.SHORT_CIRCUITED));
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestThreadPoolRejection()
        {
            TestCounterType(HystrixRollingNumberEvent.THREAD_POOL_REJECTED);
        }

        [Fact]
        public void TestFallbackSuccess()
        {
            TestCounterType(HystrixRollingNumberEvent.FALLBACK_SUCCESS);
        }

        [Fact]
        public void TestFallbackFailure()
        {
            TestCounterType(HystrixRollingNumberEvent.FALLBACK_FAILURE);
        }

        [Fact]
        public void TestExceptionThrow()
        {
            TestCounterType(HystrixRollingNumberEvent.EXCEPTION_THROWN);
        }

        [Fact]
        public void TestIncrementInMultipleBuckets()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.TIMEOUT);
                counter.Increment(HystrixRollingNumberEvent.TIMEOUT);
                counter.Increment(HystrixRollingNumberEvent.SHORT_CIRCUITED);

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds * 3);

                // Increment
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.TIMEOUT);
                counter.Increment(HystrixRollingNumberEvent.SHORT_CIRCUITED);

                // we should have 4 buckets
                Assert.Equal(4, counter._buckets.Size);

                // the counts of the last bucket
                Assert.Equal(2, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.SUCCESS).Sum());
                Assert.Equal(3, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.FAILURE).Sum());
                Assert.Equal(1, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.TIMEOUT).Sum());
                Assert.Equal(1, counter._buckets.Last.GetAdder(HystrixRollingNumberEvent.SHORT_CIRCUITED).Sum());

                // the total counts
                Assert.Equal(6, counter.GetRollingSum(HystrixRollingNumberEvent.SUCCESS));
                Assert.Equal(5, counter.GetRollingSum(HystrixRollingNumberEvent.FAILURE));
                Assert.Equal(3, counter.GetRollingSum(HystrixRollingNumberEvent.TIMEOUT));
                Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.SHORT_CIRCUITED));

                // wait until window passes
                time.Increment(counter._timeInMilliseconds);

                // Increment
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);

                // the total counts should now include only the last bucket after a reset since the window passed
                Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.SUCCESS));
                Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.FAILURE));
                Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.TIMEOUT));
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestCounterRetrievalRefreshesBuckets()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);
                counter.Increment(HystrixRollingNumberEvent.FAILURE);

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds * 3);

                // we should have 1 bucket since nothing has triggered the update of buckets in the elapsed time
                Assert.Equal(1, counter._buckets.Size);

                // the total counts
                Assert.Equal(4, counter.GetRollingSum(HystrixRollingNumberEvent.SUCCESS));
                Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.FAILURE));

                // we should have 4 buckets as the counter 'gets' should have triggered the buckets being created to fill in time
                Assert.Equal(4, counter._buckets.Size);

                // wait until window passes
                time.Increment(counter._timeInMilliseconds);

                // the total counts should all be 0 (and the buckets cleared by the get, not only Increment)
                Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.SUCCESS));
                Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.FAILURE));

                // Increment
                counter.Increment(HystrixRollingNumberEvent.SUCCESS);

                // the total counts should now include only the last bucket after a reset since the window passed
                Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.SUCCESS));
                Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.FAILURE));
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestUpdateMax1()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 10);

                // we should have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // the count should be 10
                Assert.Equal(10, counter._buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE).Max);
                Assert.Equal(10, counter.GetRollingMaxValue(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE));

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds * 3);

                // Increment again in latest bucket
                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 20);

                // we should have 4 buckets
                Assert.Equal(4, counter._buckets.Size);

                // the max
                Assert.Equal(20, counter._buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE).Max);

                // counts per bucket
                var values = counter.GetValues(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE);
                Assert.Equal(10, values[0]); // oldest bucket
                Assert.Equal(0, values[1]);
                Assert.Equal(0, values[2]);
                Assert.Equal(20, values[3]); // latest bucket
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestUpdateMax2()
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 10);
                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 30);
                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 20);

                // we should have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // the count should be 30
                Assert.Equal(30, counter._buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE).Max);
                Assert.Equal(30, counter.GetRollingMaxValue(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE));

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds * 3);

                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 30);
                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 30);
                counter.UpdateRollingMax(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE, 50);

                // we should have 4 buckets
                Assert.Equal(4, counter._buckets.Size);

                // the count
                Assert.Equal(50, counter._buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE).Max);
                Assert.Equal(50, counter.GetValueOfLatestBucket(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE));

                // values per bucket
                var values = counter.GetValues(HystrixRollingNumberEvent.THREAD_MAX_ACTIVE);
                Assert.Equal(30, values[0]); // oldest bucket
                Assert.Equal(0, values[1]);
                Assert.Equal(0, values[2]);
                Assert.Equal(50, values[3]); // latest bucket
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestMaxValue()
        {
            var time = new MockedTime();
            try
            {
                var type = HystrixRollingNumberEvent.THREAD_MAX_ACTIVE;

                var counter = new HystrixRollingNumber(time, 200, 10);

                counter.UpdateRollingMax(type, 10);

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds);

                counter.UpdateRollingMax(type, 30);

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds);

                counter.UpdateRollingMax(type, 40);

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds);

                counter.UpdateRollingMax(type, 15);

                Assert.Equal(40, counter.GetRollingMaxValue(type));
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }

        [Fact]
        public void TestEmptySum()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.COLLAPSED;
            var counter = new HystrixRollingNumber(time, 200, 10);
            Assert.Equal(0, counter.GetRollingSum(type));
        }

        [Fact]
        public void TestEmptyMax()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.THREAD_MAX_ACTIVE;
            var counter = new HystrixRollingNumber(time, 200, 10);
            Assert.Equal(0, counter.GetRollingMaxValue(type));
        }

        [Fact]
        public void TestEmptyLatestValue()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.THREAD_MAX_ACTIVE;
            var counter = new HystrixRollingNumber(time, 200, 10);
            Assert.Equal(0, counter.GetValueOfLatestBucket(type));
        }

        [Fact]
        public void TestRolling()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.THREAD_MAX_ACTIVE;
            var counter = new HystrixRollingNumber(time, 20, 2);

            // iterate over 20 buckets on a queue sized for 2
            for (var i = 0; i < 20; i++)
            {
                // first bucket
                counter.GetCurrentBucket();
                try
                {
                    time.Increment(counter._bucketSizeInMillseconds);
                }
                catch (Exception)
                {
                    // ignore
                }

                Assert.Equal(2, counter.GetValues(type).Length);

                counter.GetValueOfLatestBucket(type);

                // System.out.println("Head: " + counter._buckets.state.get().head);
                // System.out.println("Tail: " + counter._buckets.state.get().tail);
            }
        }

        [Fact]
        public void TestCumulativeCounterAfterRolling()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.SUCCESS;
            var counter = new HystrixRollingNumber(time, 20, 2);

            Assert.Equal(0, counter.GetCumulativeSum(type));

            // iterate over 20 buckets on a queue sized for 2
            for (var i = 0; i < 20; i++)
            {
                // first bucket
                counter.Increment(type);
                try
                {
                    time.Increment(counter._bucketSizeInMillseconds);
                }
                catch (Exception)
                {
                    // ignore
                }

                Assert.Equal(2, counter.GetValues(type).Length);

                counter.GetValueOfLatestBucket(type);
            }

            // cumulative count should be 20 (for the number of loops above) regardless of buckets rolling
            Assert.Equal(20, counter.GetCumulativeSum(type));
        }

        [Fact]
        public void TestCumulativeCounterAfterRollingAndReset()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.SUCCESS;
            var counter = new HystrixRollingNumber(time, 20, 2);

            Assert.Equal(0, counter.GetCumulativeSum(type));

            // iterate over 20 buckets on a queue sized for 2
            for (var i = 0; i < 20; i++)
            {
                // first bucket
                counter.Increment(type);
                try
                {
                    time.Increment(counter._bucketSizeInMillseconds);
                }
                catch (Exception)
                {
                    // ignore
                }

                Assert.Equal(2, counter.GetValues(type).Length);

                counter.GetValueOfLatestBucket(type);

                if (i == 5 || i == 15)
                {
                    // simulate a reset occurring every once in a while
                    // so we ensure the absolute Sum is handling it okay
                    counter.Reset();
                }
            }

            // cumulative count should be 20 (for the number of loops above) regardless of buckets rolling
            Assert.Equal(20, counter.GetCumulativeSum(type));
        }

        [Fact]
        public void TestCumulativeCounterAfterRollingAndReset2()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.SUCCESS;
            var counter = new HystrixRollingNumber(time, 20, 2);

            Assert.Equal(0, counter.GetCumulativeSum(type));

            counter.Increment(type);
            counter.Increment(type);
            counter.Increment(type);

            // iterate over 20 buckets on a queue sized for 2
            for (var i = 0; i < 20; i++)
            {
                try
                {
                    time.Increment(counter._bucketSizeInMillseconds);
                }
                catch (Exception)
                {
                    // ignore
                }

                if (i == 5 || i == 15)
                {
                    // simulate a reset occurring every once in a while
                    // so we ensure the absolute Sum is handling it okay
                    counter.Reset();
                }
            }

            // no Increments during the loop, just some before and after
            counter.Increment(type);
            counter.Increment(type);

            // cumulative count should be 5 regardless of buckets rolling
            Assert.Equal(5, counter.GetCumulativeSum(type));
        }

        [Fact]
        public void TestCumulativeCounterAfterRollingAndReset3()
        {
            var time = new MockedTime();
            var type = HystrixRollingNumberEvent.SUCCESS;
            var counter = new HystrixRollingNumber(time, 20, 2);

            Assert.Equal(0, counter.GetCumulativeSum(type));

            counter.Increment(type);
            counter.Increment(type);
            counter.Increment(type);

            // iterate over 20 buckets on a queue sized for 2
            for (var i = 0; i < 20; i++)
            {
                try
                {
                    time.Increment(counter._bucketSizeInMillseconds);
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            // since we are rolling over the buckets it should reset naturally

            // no Increments during the loop, just some before and after
            counter.Increment(type);
            counter.Increment(type);

            // cumulative count should be 5 regardless of buckets rolling
            Assert.Equal(5, counter.GetCumulativeSum(type));
        }

        private void TestCounterType(HystrixRollingNumberEvent type)
        {
            var time = new MockedTime();
            try
            {
                var counter = new HystrixRollingNumber(time, 200, 10);

                // Increment
                counter.Increment(type);

                // we should have 1 bucket
                Assert.Equal(1, counter._buckets.Size);

                // the count should be 1
                Assert.Equal(1, counter._buckets.Last.GetAdder(type).Sum());
                Assert.Equal(1, counter.GetRollingSum(type));

                // sleep to get to a new bucket
                time.Increment(counter._bucketSizeInMillseconds * 3);

                // Increment again in latest bucket
                counter.Increment(type);

                // we should have 4 buckets
                Assert.Equal(4, counter._buckets.Size);

                // the counts of the last bucket
                Assert.Equal(1, counter._buckets.Last.GetAdder(type).Sum());

                // the total counts
                Assert.Equal(2, counter.GetRollingSum(type));
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.True(false, "Exception: " + e.Message);
            }
        }
    }
}

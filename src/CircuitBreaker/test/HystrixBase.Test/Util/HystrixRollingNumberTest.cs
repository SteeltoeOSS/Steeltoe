// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test;

public class HystrixRollingNumberTest
{
    private readonly ITestOutputHelper _output;

    public HystrixRollingNumberTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestCreatesBuckets()
    {
        var time = new MockedTime();
        try
        {
            var counter = new HystrixRollingNumber(time, 200, 10);

            // confirm the initial settings
            Assert.Equal(200, counter.TimeInMilliseconds);
            Assert.Equal(10, counter.NumberOfBuckets);
            Assert.Equal(20, counter.BucketSizeInMilliseconds);

            // we start out with 0 buckets in the queue
            Assert.Equal(0, counter.Buckets.Size);

            // add a success in each interval which should result in all 10 buckets being created with 1 success in each
            for (var i = 0; i < counter.NumberOfBuckets; i++)
            {
                counter.Increment(HystrixRollingNumberEvent.Success);
                time.Increment(counter.BucketSizeInMilliseconds);
            }

            // confirm we have all 10 buckets
            Assert.Equal(10, counter.Buckets.Size);

            // add 1 more and we should still only have 10 buckets since that's the max
            counter.Increment(HystrixRollingNumberEvent.Success);
            Assert.Equal(10, counter.Buckets.Size);
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            Assert.Equal(0, counter.Buckets.Size);

            // add 1
            counter.Increment(HystrixRollingNumberEvent.Success);

            // confirm we have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // confirm we still have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // add 1
            counter.Increment(HystrixRollingNumberEvent.Success);

            // we should now have a single bucket with no values in it instead of 2 or more buckets
            Assert.Equal(1, counter.Buckets.Size);
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            counter.Increment(HystrixRollingNumberEvent.Success);

            // we should have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // wait past 3 bucket time periods (the 1st bucket then 2 empty ones)
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            // add another
            counter.Increment(HystrixRollingNumberEvent.Success);

            // we should have 4 (1 + 2 empty + 1 new one) buckets
            Assert.Equal(4, counter.Buckets.Size);
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Timeout);

            // we should have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // the count should be 4
            Assert.Equal(4, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Success).Sum());
            Assert.Equal(2, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Failure).Sum());
            Assert.Equal(1, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Timeout).Sum());
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            counter.Increment(HystrixRollingNumberEvent.Timeout);

            // we should have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // the count should be 1
            Assert.Equal(1, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Timeout).Sum());
            Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.Timeout));

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            // increment again in latest bucket
            counter.Increment(HystrixRollingNumberEvent.Timeout);

            // we should have 4 buckets
            Assert.Equal(4, counter.Buckets.Size);

            // the counts of the last bucket
            Assert.Equal(1, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Timeout).Sum());

            // the total counts
            Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.Timeout));
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            counter.Increment(HystrixRollingNumberEvent.ShortCircuited);

            // we should have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // the count should be 1
            Assert.Equal(1, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.ShortCircuited).Sum());
            Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.ShortCircuited));

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            // increment again in latest bucket
            counter.Increment(HystrixRollingNumberEvent.ShortCircuited);

            // we should have 4 buckets
            Assert.Equal(4, counter.Buckets.Size);

            // the counts of the last bucket
            Assert.Equal(1, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.ShortCircuited).Sum());

            // the total counts
            Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.ShortCircuited));
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
        }
    }

    [Fact]
    public void TestThreadPoolRejection()
    {
        TestCounterType(HystrixRollingNumberEvent.ThreadPoolRejected);
    }

    [Fact]
    public void TestFallbackSuccess()
    {
        TestCounterType(HystrixRollingNumberEvent.FallbackSuccess);
    }

    [Fact]
    public void TestFallbackFailure()
    {
        TestCounterType(HystrixRollingNumberEvent.FallbackFailure);
    }

    [Fact]
    public void TestExceptionThrow()
    {
        TestCounterType(HystrixRollingNumberEvent.ExceptionThrown);
    }

    [Fact]
    public void TestIncrementInMultipleBuckets()
    {
        var time = new MockedTime();
        try
        {
            var counter = new HystrixRollingNumber(time, 200, 10);

            // Increment
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Timeout);
            counter.Increment(HystrixRollingNumberEvent.Timeout);
            counter.Increment(HystrixRollingNumberEvent.ShortCircuited);

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            // Increment
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Timeout);
            counter.Increment(HystrixRollingNumberEvent.ShortCircuited);

            // we should have 4 buckets
            Assert.Equal(4, counter.Buckets.Size);

            // the counts of the last bucket
            Assert.Equal(2, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Success).Sum());
            Assert.Equal(3, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Failure).Sum());
            Assert.Equal(1, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.Timeout).Sum());
            Assert.Equal(1, counter.Buckets.Last.GetAdder(HystrixRollingNumberEvent.ShortCircuited).Sum());

            // the total counts
            Assert.Equal(6, counter.GetRollingSum(HystrixRollingNumberEvent.Success));
            Assert.Equal(5, counter.GetRollingSum(HystrixRollingNumberEvent.Failure));
            Assert.Equal(3, counter.GetRollingSum(HystrixRollingNumberEvent.Timeout));
            Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.ShortCircuited));

            // wait until window passes
            time.Increment(counter.TimeInMilliseconds);

            // Increment
            counter.Increment(HystrixRollingNumberEvent.Success);

            // the total counts should now include only the last bucket after a reset since the window passed
            Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.Success));
            Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.Failure));
            Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.Timeout));
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Success);
            counter.Increment(HystrixRollingNumberEvent.Failure);
            counter.Increment(HystrixRollingNumberEvent.Failure);

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            // we should have 1 bucket since nothing has triggered the update of buckets in the elapsed time
            Assert.Equal(1, counter.Buckets.Size);

            // the total counts
            Assert.Equal(4, counter.GetRollingSum(HystrixRollingNumberEvent.Success));
            Assert.Equal(2, counter.GetRollingSum(HystrixRollingNumberEvent.Failure));

            // we should have 4 buckets as the counter 'gets' should have triggered the buckets being created to fill in time
            Assert.Equal(4, counter.Buckets.Size);

            // wait until window passes
            time.Increment(counter.TimeInMilliseconds);

            // the total counts should all be 0 (and the buckets cleared by the get, not only Increment)
            Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.Success));
            Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.Failure));

            // Increment
            counter.Increment(HystrixRollingNumberEvent.Success);

            // the total counts should now include only the last bucket after a reset since the window passed
            Assert.Equal(1, counter.GetRollingSum(HystrixRollingNumberEvent.Success));
            Assert.Equal(0, counter.GetRollingSum(HystrixRollingNumberEvent.Failure));
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 10);

            // we should have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // the count should be 10
            Assert.Equal(10, counter.Buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.ThreadMaxActive).Max);
            Assert.Equal(10, counter.GetRollingMaxValue(HystrixRollingNumberEvent.ThreadMaxActive));

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            // Increment again in latest bucket
            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 20);

            // we should have 4 buckets
            Assert.Equal(4, counter.Buckets.Size);

            // the max
            Assert.Equal(20, counter.Buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.ThreadMaxActive).Max);

            // counts per bucket
            var values = counter.GetValues(HystrixRollingNumberEvent.ThreadMaxActive);
            Assert.Equal(10, values[0]); // oldest bucket
            Assert.Equal(0, values[1]);
            Assert.Equal(0, values[2]);
            Assert.Equal(20, values[3]); // latest bucket
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
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
            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 10);
            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 30);
            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 20);

            // we should have 1 bucket
            Assert.Equal(1, counter.Buckets.Size);

            // the count should be 30
            Assert.Equal(30, counter.Buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.ThreadMaxActive).Max);
            Assert.Equal(30, counter.GetRollingMaxValue(HystrixRollingNumberEvent.ThreadMaxActive));

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 30);
            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 30);
            counter.UpdateRollingMax(HystrixRollingNumberEvent.ThreadMaxActive, 50);

            // we should have 4 buckets
            Assert.Equal(4, counter.Buckets.Size);

            // the count
            Assert.Equal(50, counter.Buckets.Last.GetMaxUpdater(HystrixRollingNumberEvent.ThreadMaxActive).Max);
            Assert.Equal(50, counter.GetValueOfLatestBucket(HystrixRollingNumberEvent.ThreadMaxActive));

            // values per bucket
            var values = counter.GetValues(HystrixRollingNumberEvent.ThreadMaxActive);
            Assert.Equal(30, values[0]); // oldest bucket
            Assert.Equal(0, values[1]);
            Assert.Equal(0, values[2]);
            Assert.Equal(50, values[3]); // latest bucket
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
        }
    }

    [Fact]
    public void TestMaxValue()
    {
        var time = new MockedTime();
        try
        {
            var type = HystrixRollingNumberEvent.ThreadMaxActive;

            var counter = new HystrixRollingNumber(time, 200, 10);

            counter.UpdateRollingMax(type, 10);

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds);

            counter.UpdateRollingMax(type, 30);

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds);

            counter.UpdateRollingMax(type, 40);

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds);

            counter.UpdateRollingMax(type, 15);

            Assert.Equal(40, counter.GetRollingMaxValue(type));
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
        }
    }

    [Fact]
    public void TestEmptySum()
    {
        var time = new MockedTime();
        var type = HystrixRollingNumberEvent.Collapsed;
        var counter = new HystrixRollingNumber(time, 200, 10);
        Assert.Equal(0, counter.GetRollingSum(type));
    }

    [Fact]
    public void TestEmptyMax()
    {
        var time = new MockedTime();
        var type = HystrixRollingNumberEvent.ThreadMaxActive;
        var counter = new HystrixRollingNumber(time, 200, 10);
        Assert.Equal(0, counter.GetRollingMaxValue(type));
    }

    [Fact]
    public void TestEmptyLatestValue()
    {
        var time = new MockedTime();
        var type = HystrixRollingNumberEvent.ThreadMaxActive;
        var counter = new HystrixRollingNumber(time, 200, 10);
        Assert.Equal(0, counter.GetValueOfLatestBucket(type));
    }

    [Fact]
    public void TestRolling()
    {
        var time = new MockedTime();
        var type = HystrixRollingNumberEvent.ThreadMaxActive;
        var counter = new HystrixRollingNumber(time, 20, 2);

        // iterate over 20 buckets on a queue sized for 2
        for (var i = 0; i < 20; i++)
        {
            // first bucket
            counter.GetCurrentBucket();
            try
            {
                time.Increment(counter.BucketSizeInMilliseconds);
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
        var type = HystrixRollingNumberEvent.Success;
        var counter = new HystrixRollingNumber(time, 20, 2);

        Assert.Equal(0, counter.GetCumulativeSum(type));

        // iterate over 20 buckets on a queue sized for 2
        for (var i = 0; i < 20; i++)
        {
            // first bucket
            counter.Increment(type);
            try
            {
                time.Increment(counter.BucketSizeInMilliseconds);
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
        var type = HystrixRollingNumberEvent.Success;
        var counter = new HystrixRollingNumber(time, 20, 2);

        Assert.Equal(0, counter.GetCumulativeSum(type));

        // iterate over 20 buckets on a queue sized for 2
        for (var i = 0; i < 20; i++)
        {
            // first bucket
            counter.Increment(type);
            try
            {
                time.Increment(counter.BucketSizeInMilliseconds);
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
        var type = HystrixRollingNumberEvent.Success;
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
                time.Increment(counter.BucketSizeInMilliseconds);
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
        var type = HystrixRollingNumberEvent.Success;
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
                time.Increment(counter.BucketSizeInMilliseconds);
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
            Assert.Equal(1, counter.Buckets.Size);

            // the count should be 1
            Assert.Equal(1, counter.Buckets.Last.GetAdder(type).Sum());
            Assert.Equal(1, counter.GetRollingSum(type));

            // sleep to get to a new bucket
            time.Increment(counter.BucketSizeInMilliseconds * 3);

            // Increment again in latest bucket
            counter.Increment(type);

            // we should have 4 buckets
            Assert.Equal(4, counter.Buckets.Size);

            // the counts of the last bucket
            Assert.Equal(1, counter.Buckets.Last.GetAdder(type).Sum());

            // the total counts
            Assert.Equal(2, counter.GetRollingSum(type));
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.True(false, $"Exception: {e.Message}");
        }
    }
}

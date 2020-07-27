// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Steeltoe.CircuitBreaker.Hystrix.Util.HystrixRollingPercentile;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test
{
    public class HystrixRollingPercentileTest
    {
        private const bool Enabled = true;
        private static readonly int TimeInMilliseconds = 60000;
        private static readonly int NumberOfBuckets = 12; // 12 buckets at 5000ms each
        private static readonly int BucketDataLength = 1000;
        private ITestOutputHelper output;

        public HystrixRollingPercentileTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestRolling()
        {
            var time = new MockedTime();
            var p = new HystrixRollingPercentile(time, TimeInMilliseconds, NumberOfBuckets, BucketDataLength, Enabled);
            p.AddValue(1000);
            p.AddValue(1000);
            p.AddValue(1000);
            p.AddValue(2000);

            Assert.Equal(1, p._buckets.Size);

            // no bucket turnover yet so percentile not yet generated
            Assert.Equal(0, p.GetPercentile(50));

            time.Increment(6000);

            // still only 1 bucket until we touch it again
            Assert.Equal(1, p._buckets.Size);

            // a bucket has been created so we have a new percentile
            Assert.Equal(1000, p.GetPercentile(50));

            // now 2 buckets since getting a percentile causes bucket retrieval
            Assert.Equal(2, p._buckets.Size);

            p.AddValue(1000);
            p.AddValue(500);

            // should still be 2 buckets
            Assert.Equal(2, p._buckets.Size);

            p.AddValue(200);
            p.AddValue(200);
            p.AddValue(1600);
            p.AddValue(200);
            p.AddValue(1600);
            p.AddValue(1600);

            // we haven't progressed to a new bucket so the percentile should be the same and ignore the most recent bucket
            Assert.Equal(1000, p.GetPercentile(50));

            // Increment to another bucket so we include all of the above in the PercentileSnapshot
            time.Increment(6000);

            // the rolling version should have the same data as creating a snapshot like this
            var ps = new PercentileSnapshot(1000, 1000, 1000, 2000, 1000, 500, 200, 200, 1600, 200, 1600, 1600);

            Assert.Equal(ps.GetPercentile(0.15), p.GetPercentile(0.15));
            Assert.Equal(ps.GetPercentile(0.50), p.GetPercentile(0.50));
            Assert.Equal(ps.GetPercentile(0.90), p.GetPercentile(0.90));
            Assert.Equal(ps.GetPercentile(0.995), p.GetPercentile(0.995));

            output.WriteLine("100th: " + ps.GetPercentile(100) + "  " + p.GetPercentile(100));
            output.WriteLine("99.5th: " + ps.GetPercentile(99.5) + "  " + p.GetPercentile(99.5));
            output.WriteLine("99th: " + ps.GetPercentile(99) + "  " + p.GetPercentile(99));
            output.WriteLine("90th: " + ps.GetPercentile(90) + "  " + p.GetPercentile(90));
            output.WriteLine("50th: " + ps.GetPercentile(50) + "  " + p.GetPercentile(50));
            output.WriteLine("10th: " + ps.GetPercentile(10) + "  " + p.GetPercentile(10));

            // mean = 1000+1000+1000+2000+1000+500+200+200+1600+200+1600+1600/12
            Assert.Equal(991, ps.Mean);
        }

        [Fact]
        public void TestValueIsZeroAfterRollingWindowPassesAndNoTraffic()
        {
            var time = new MockedTime();
            var p = new HystrixRollingPercentile(time, TimeInMilliseconds, NumberOfBuckets, BucketDataLength, Enabled);
            p.AddValue(1000);
            p.AddValue(1000);
            p.AddValue(1000);
            p.AddValue(2000);
            p.AddValue(4000);

            Assert.Equal(1, p._buckets.Size);

            // no bucket turnover yet so percentile not yet generated
            Assert.Equal(0, p.GetPercentile(50));

            time.Increment(6000);

            // still only 1 bucket until we touch it again
            Assert.Equal(1, p._buckets.Size);

            // a bucket has been created so we have a new percentile
            Assert.Equal(1500, p.GetPercentile(50));

            // let 1 minute pass
            time.Increment(60000);

            // no data in a minute should mean all buckets are empty (or reset) so we should not have any percentiles
            Assert.Equal(0, p.GetPercentile(50));
        }

        [Fact]
        public void TestSampleDataOverTime1()
        {
            output.WriteLine("\n\n***************************** testSampleDataOverTime1 \n");

            var time = new MockedTime();
            var p = new HystrixRollingPercentile(time, TimeInMilliseconds, NumberOfBuckets, BucketDataLength, Enabled);
            var previousTime = 0;
            for (var i = 0; i < SampleDataHolder1.Data.Length; i++)
            {
                var timeInMillisecondsSinceStart = SampleDataHolder1.Data[i][0];
                var latency = SampleDataHolder1.Data[i][1];
                time.Increment(timeInMillisecondsSinceStart - previousTime);
                previousTime = timeInMillisecondsSinceStart;
                p.AddValue(latency);
            }

            output.WriteLine("0.01: " + p.GetPercentile(0.01));
            output.WriteLine("Median: " + p.GetPercentile(50));
            output.WriteLine("90th: " + p.GetPercentile(90));
            output.WriteLine("99th: " + p.GetPercentile(99));
            output.WriteLine("99.5th: " + p.GetPercentile(99.5));
            output.WriteLine("99.99: " + p.GetPercentile(99.99));

            output.WriteLine("Median: " + p.GetPercentile(50));
            output.WriteLine("Median: " + p.GetPercentile(50));
            output.WriteLine("Median: " + p.GetPercentile(50));

            /*
             * In a loop as a use case was found where very different values were calculated in subsequent requests.
             */
            for (var i = 0; i < 10; i++)
            {
                if (p.GetPercentile(50) > 5)
                {
                    Assert.True(false, "We expect around 2 but got: " + p.GetPercentile(50));
                }

                if (p.GetPercentile(99.5) < 20)
                {
                    Assert.True(false, "We expect to see some high values over 20 but got: " + p.GetPercentile(99.5));
                }
            }
        }

        [Fact]
        public void TestSampleDataOverTime2()
        {
            output.WriteLine("\n\n***************************** testSampleDataOverTime2 \n");
            var time = new MockedTime();
            var previousTime = 0;
            var p = new HystrixRollingPercentile(time, TimeInMilliseconds, NumberOfBuckets, BucketDataLength, Enabled);
            for (var i = 0; i < SampleDataHolder2.Data.Length; i++)
            {
                var timeInMillisecondsSinceStart = SampleDataHolder2.Data[i][0];
                var latency = SampleDataHolder2.Data[i][1];
                time.Increment(timeInMillisecondsSinceStart - previousTime);
                previousTime = timeInMillisecondsSinceStart;
                p.AddValue(latency);
            }

            output.WriteLine("0.01: " + p.GetPercentile(0.01));
            output.WriteLine("Median: " + p.GetPercentile(50));
            output.WriteLine("90th: " + p.GetPercentile(90));
            output.WriteLine("99th: " + p.GetPercentile(99));
            output.WriteLine("99.5th: " + p.GetPercentile(99.5));
            output.WriteLine("99.99: " + p.GetPercentile(99.99));

            if (p.GetPercentile(50) > 90 || p.GetPercentile(50) < 50)
            {
                Assert.True(false, "We expect around 60-70 but got: " + p.GetPercentile(50));
            }

            if (p.GetPercentile(99) < 400)
            {
                Assert.True(false, "We expect to see some high values over 400 but got: " + p.GetPercentile(99));
            }
        }

        [Fact]
        public void TestPercentileAlgorithm_Median1()
        {
            var list = new PercentileSnapshot(100, 100, 100, 100, 200, 200, 200, 300, 300, 300, 300);
            Assert.Equal(200, list.GetPercentile(50));
        }

        [Fact]
        public void TestPercentileAlgorithm_Median2()
        {
            var list = new PercentileSnapshot(100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 500);
            Assert.Equal(100, list.GetPercentile(50));
        }

        [Fact]
        public void TestPercentileAlgorithm_Median3()
        {
            var list = new PercentileSnapshot(50, 75, 100, 125, 160, 170, 180, 200, 210, 300, 500);
            Assert.Equal(175, list.GetPercentile(50));
        }

        [Fact]
        public void TestPercentileAlgorithm_Median4()
        {
            var list = new PercentileSnapshot(300, 75, 125, 500, 100, 160, 180, 200, 210, 50, 170);
            Assert.Equal(175, list.GetPercentile(50));
        }

        [Fact]
        public void TestPercentileAlgorithm_Extremes()
        {
            var p = new PercentileSnapshot(2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 800, 768, 657, 700, 867);

            output.WriteLine("0.01: " + p.GetPercentile(0.01));
            output.WriteLine("10th: " + p.GetPercentile(10));
            output.WriteLine("Median: " + p.GetPercentile(50));
            output.WriteLine("75th: " + p.GetPercentile(75));
            output.WriteLine("90th: " + p.GetPercentile(90));
            output.WriteLine("99th: " + p.GetPercentile(99));
            output.WriteLine("99.5th: " + p.GetPercentile(99.5));
            output.WriteLine("99.99: " + p.GetPercentile(99.99));
            Assert.Equal(2, p.GetPercentile(50));
            Assert.Equal(2, p.GetPercentile(10));
            Assert.Equal(2, p.GetPercentile(75));
            if (p.GetPercentile(95) < 600)
            {
                Assert.True(false, "We expect the 90th to be over 600 to show the extremes but got: " + p.GetPercentile(90));
            }

            if (p.GetPercentile(99) < 600)
            {
                Assert.True(false, "We expect the 99th to be over 600 to show the extremes but got: " + p.GetPercentile(99));
            }
        }

        [Fact]
        public void TestPercentileAlgorithm_HighPercentile()
        {
            var p = GetPercentileForValues(1, 2, 3);
            Assert.Equal(2, p.GetPercentile(50));
            Assert.Equal(3, p.GetPercentile(75));
        }

        [Fact]
        public void TestPercentileAlgorithm_LowPercentile()
        {
            var p = GetPercentileForValues(1, 2);
            Assert.Equal(1, p.GetPercentile(25));
            Assert.Equal(2, p.GetPercentile(75));
        }

        [Fact]
        public void TestPercentileAlgorithm_Percentiles()
        {
            var p = GetPercentileForValues(10, 30, 20, 40);
            Assert.Equal(22, p.GetPercentile(30));
            Assert.Equal(20, p.GetPercentile(25));
            Assert.Equal(40, p.GetPercentile(75));
            Assert.Equal(30, p.GetPercentile(50));

            // invalid percentiles
            Assert.Equal(10, p.GetPercentile(-1));
            Assert.Equal(40, p.GetPercentile(101));
        }

        [Fact]
        public void TestPercentileAlgorithm_NISTExample()
        {
            var p = GetPercentileForValues(951772, 951567, 951937, 951959, 951442, 950610, 951591, 951195, 951772, 950925, 951990, 951682);
            Assert.Equal(951983, p.GetPercentile(90));
            Assert.Equal(951990, p.GetPercentile(100));
        }

        [Fact]
        public void TestDoesNothingWhenDisabled()
        {
            var time = new MockedTime();
            var previousTime = 0;
            var p = new HystrixRollingPercentile(time, TimeInMilliseconds, NumberOfBuckets, BucketDataLength, false);
            for (var i = 0; i < SampleDataHolder2.Data.Length; i++)
            {
                var timeInMillisecondsSinceStart = SampleDataHolder2.Data[i][0];
                var latency = SampleDataHolder2.Data[i][1];
                time.Increment(timeInMillisecondsSinceStart - previousTime);
                previousTime = timeInMillisecondsSinceStart;
                p.AddValue(latency);
            }

            Assert.Equal(-1, p.GetPercentile(50));
            Assert.Equal(-1, p.GetPercentile(75));
            Assert.Equal(-1, p.Mean);
        }

        [Fact]
        public void TestThreadSafety()
        {
            var time = new MockedTime();
            var p = new HystrixRollingPercentile(time, 100, 25, 1000, true);

            var num_threads = 1000;  // .NET Core StackOverflow
            var num_iterations = 1000000;

            var latch = new CountdownEvent(num_threads);

            var aggregateMetrics = new AtomicInteger(); // same as a blackhole

            var r = new Random();
            var cts = new CancellationTokenSource();
            var metricsPoller = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    aggregateMetrics.AddAndGet(p.Mean + p.GetPercentile(10) + p.GetPercentile(50) + p.GetPercentile(90));
                }
            });

            for (var i = 0; i < num_threads; i++)
            {
                var threadId = i;
                Task.Run(() =>
                {
                    for (var j = 1; j < (num_iterations / num_threads) + 1; j++)
                    {
                        var nextInt = r.Next(100);
                        p.AddValue(nextInt);
                        if (threadId == 0)
                        {
                            time.Increment(1);
                        }
                    }

                    latch.SignalEx();
                });
            }

            try
            {
                latch.Wait(TimeSpan.FromSeconds(100));
                cts.Cancel();
            }
            catch (Exception)
            {
                Assert.True(false, "Timeout on all threads writing percentiles");
            }

            aggregateMetrics.AddAndGet(p.Mean + p.GetPercentile(10) + p.GetPercentile(50) + p.GetPercentile(90));
            output.WriteLine(p.Mean + " : " + p.GetPercentile(50) + " : " + p.GetPercentile(75) + " : " + p.GetPercentile(90) + " : " + p.GetPercentile(95) + " : " + p.GetPercentile(99));
        }

        [Fact]
        public void TestWriteThreadSafety()
        {
            var time = new MockedTime();
            var p = new HystrixRollingPercentile(time, 100, 25, 1000, true);

            var num_threads = 10;
            var num_iterations = 1000;

            var latch = new CountdownEvent(num_threads);

            var r = new Random();

            var added = new AtomicInteger(0);

            for (var i = 0; i < num_threads; i++)
            {
                var t = new Task(
                    () =>
                    {
                        for (var j = 1; j < (num_iterations / num_threads) + 1; j++)
                        {
                            var nextInt = r.Next(100);
                            p.AddValue(nextInt);
                            added.GetAndIncrement();
                        }

                        latch.SignalEx();
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning);
                t.Start();
            }

            try
            {
                latch.Wait(TimeSpan.FromSeconds(100));
                Assert.Equal(added.Value, p._buckets.PeekLast._data.Length);
            }
            catch (Exception)
            {
                Assert.True(false, "Timeout on all threads writing percentiles");
            }
        }

        [Fact]
        public void TestThreadSafetyMulti()
        {
            for (var i = 0; i < 100; i++)
            {
                TestThreadSafety();
            }

            Assert.True(true, "Nothing blew up");
        }

        internal PercentileSnapshot GetPercentileForValues(params int[] values)
        {
            return new PercentileSnapshot(values);
        }
    }
}

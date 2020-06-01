// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixThreadPoolMetricsTest : HystrixTestBase, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("HystrixThreadPoolMetrics-UnitTest");
        private static readonly IHystrixThreadPoolKey TpKey = HystrixThreadPoolKeyDefault.AsKey("HystrixThreadPoolMetrics-ThreadPool");
        private readonly ITestOutputHelper output;

        public HystrixThreadPoolMetricsTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            HystrixThreadPoolMetrics.Reset();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void ShouldYieldNoExecutedTasksOnStartup()
        {
            // given
            ICollection<HystrixThreadPoolMetrics> instances = HystrixThreadPoolMetrics.GetInstances();

            // then
            Assert.Equal(0, instances.Count);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task ShouldReturnOneExecutedTask()
        {
            // given
            var stream = RollingThreadPoolEventCounterStream.GetInstance(TpKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            var cmd = new NoOpHystrixCommand(output);
            await cmd.ExecuteAsync();
            Time.Wait(250);

            ICollection<HystrixThreadPoolMetrics> instances = HystrixThreadPoolMetrics.GetInstances();

            // then
            output.WriteLine($"Instance count: {instances.Count}");
            Assert.Equal(1, instances.Count);
            HystrixThreadPoolMetrics metrics = instances.First();
            output.WriteLine($"RollingCountThreadsExecuted: {metrics.RollingCountThreadsExecuted}");
            Assert.Equal(1, metrics.RollingCountThreadsExecuted);
        }

        private class NoOpHystrixCommand : HystrixCommand<bool>
        {
            private readonly ITestOutputHelper output;

            public NoOpHystrixCommand(ITestOutputHelper output)
                : base(GetCommandOptions())
            {
                this.output = output;
            }

            protected override bool Run()
            {
                output.WriteLine("Run in thread : " + Thread.CurrentThread.ManagedThreadId);
                return false;
            }

            private static IHystrixThreadPoolOptions GetThreadPoolOptions()
            {
                HystrixThreadPoolOptions opts = new HystrixThreadPoolOptions(TpKey)
                {
                    MetricsRollingStatisticalWindowInMilliseconds = 100
                };
                return opts;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                HystrixCommandOptions opts = new HystrixCommandOptions()
                {
                    GroupKey = GroupKey,
                    ThreadPoolKey = TpKey,
                    ThreadPoolOptions = GetThreadPoolOptions()
                };
                return opts;
            }
        }
    }
}

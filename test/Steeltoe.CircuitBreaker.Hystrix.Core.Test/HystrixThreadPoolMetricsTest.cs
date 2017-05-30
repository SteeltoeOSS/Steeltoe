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

using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixThreadPoolMetricsTest : HystrixTestBase, IDisposable
    {
        private static IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("HystrixThreadPoolMetrics-UnitTest");
        private static IHystrixThreadPoolKey tpKey = HystrixThreadPoolKeyDefault.AsKey("HystrixThreadPoolMetrics-ThreadPool");
        ITestOutputHelper output;

        public HystrixThreadPoolMetricsTest(ITestOutputHelper output) : base()
        {
            this.output = output;
            HystrixThreadPoolMetrics.Reset();
        }

        [Fact]
        public void ShouldYieldNoExecutedTasksOnStartup()
        {
            //given
            ICollection<HystrixThreadPoolMetrics> instances = HystrixThreadPoolMetrics.GetInstances();

            //then
            Assert.Equal(0, instances.Count);

        }
        [Fact]
        public void ShouldReturnOneExecutedTask()
        {
            //given
            var stream = RollingThreadPoolEventCounterStream.GetInstance(tpKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            var cmd = new NoOpHystrixCommand(output);
            cmd.Execute();
            Time.Wait( 125);

            ICollection<HystrixThreadPoolMetrics> instances = HystrixThreadPoolMetrics.GetInstances();

            //then
            Assert.Equal(1, instances.Count);
            HystrixThreadPoolMetrics metrics = instances.First();
            Assert.Equal(1, metrics.RollingCountThreadsExecuted);
    

        }
        class NoOpHystrixCommand : HystrixCommand<bool> {
            ITestOutputHelper output;
            public NoOpHystrixCommand(ITestOutputHelper output) :
                    base(GetCommandOptions(), GetThreadPoolOptions())
            {
                this.output = output;
            }
        private static IHystrixCommandOptions GetCommandOptions()
        {
            HystrixCommandOptions opts = new HystrixCommandOptions()
            {
                GroupKey = groupKey,
                ThreadPoolKey = tpKey,

            };
            return opts;
        }
        private static IHystrixThreadPoolOptions GetThreadPoolOptions()
        {
            HystrixThreadPoolOptions opts = new HystrixThreadPoolOptions(tpKey)
            {
                MetricsRollingStatisticalWindowInMilliseconds = 100
            };
            return opts;
        }
        
        protected override bool Run() 
        {
            output.WriteLine("Run in thread : " + Thread.CurrentThread.ManagedThreadId);
            return false;
        }
}
}
}

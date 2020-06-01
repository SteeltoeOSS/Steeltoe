// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.CircuitBreaker.Hystrix.ThreadPool;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixTestBase : IDisposable
    {
        protected HystrixRequestContext context;

        public HystrixTestBase()
        {
            Before();
        }

        public void Before()
        {
            this.context = HystrixRequestContext.InitializeContext();

            HystrixCommandMetrics.Reset();
            HystrixThreadPoolMetrics.Reset();
            HystrixCollapserMetrics.Reset();

            // clear collapsers
            RequestCollapserFactory.Reset();

            // clear circuit breakers
            HystrixCircuitBreakerFactory.Reset();
            HystrixPlugins.Reset();
            HystrixOptionsFactory.Reset();

            // clear up all streams
            CumulativeCollapserEventCounterStream.Reset();
            CumulativeCommandEventCounterStream.Reset();
            CumulativeThreadPoolEventCounterStream.Reset();
            RollingCollapserBatchSizeDistributionStream.Reset();
            RollingCollapserEventCounterStream.Reset();
            RollingCommandEventCounterStream.Reset();
            RollingCommandLatencyDistributionStream.Reset();
            RollingCommandMaxConcurrencyStream.Reset();
            RollingCommandUserLatencyDistributionStream.Reset();
            RollingThreadPoolEventCounterStream.Reset();
            RollingThreadPoolMaxConcurrencyStream.Reset();
        }

        public void Reset()
        {
            Dispose();
            Before();
        }

        public virtual void Dispose()
        {
            if (this.context != null)
            {
                this.context.Dispose();
                this.context = null;
            }

            HystrixThreadPoolFactory.Shutdown();
        }

        public virtual bool WaitForHealthCountToUpdate(string commandKey, int maxTimeToWait, ITestOutputHelper output = null)
        {
            return WaitForHealthCountToUpdate(commandKey, 1, maxTimeToWait, output);
        }

        public virtual bool WaitForHealthCountToUpdate(string commandKey, int numberOfUpdates, int maxTimeToWait, ITestOutputHelper output = null)
        {
            var stream = HealthCountsStream.GetInstance(commandKey);
            if (stream == null)
            {
                return false;
            }

            return WaitForObservableToUpdate(stream.Observe(), numberOfUpdates, maxTimeToWait, output);
        }

        public virtual bool WaitForObservableToUpdate<T>(IObservable<T> observable, int numberOfUpdates, int maxTimeToWait, ITestOutputHelper output = null)
        {
            bool updated = false;
            int number = numberOfUpdates;

            using (observable.Subscribe((item) =>
            {
                number--;
                if (number <= 0)
                {
                    updated = true;
                    output?.WriteLine("WaitForObservableToUpdate @ " + Time.CurrentTimeMillis + " : required updates received");
                }

                output?.WriteLine("WaitForObservableToUpdate @ " + Time.CurrentTimeMillis + " : " + item.ToString());
                output?.WriteLine("WaitForObservableToUpdate ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }))
            {
                output?.WriteLine("WaitForObservableToUpdate @ " + Time.CurrentTimeMillis + " : Starting wait");
                return Time.WaitUntil(() => updated, maxTimeToWait);
            }
        }

        public virtual bool WaitForLatchedObserverToUpdate<T>(TestObserverBase<T> observer, int count, int maxWaitTime, ITestOutputHelper output = null)
        {
            var current = observer.TickCount;
            int countToWait = count;

            output?.WriteLine("WaitForObservableToUpdate ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output?.WriteLine("WaitForLatchedObserverToUpdate @ " + Time.CurrentTimeMillis + " Starting wait");
            return Time.WaitUntil(() => observer.TickCount >= current + countToWait, maxWaitTime);
        }

        public virtual bool WaitForLatchedObserverToUpdate<T>(TestObserverBase<T> observer, int count, int minWaitTime, int maxWaitTime, ITestOutputHelper output = null)
        {
            var current = observer.TickCount;
            int countToWait = count;
            long minTime = Time.CurrentTimeMillis + minWaitTime;

            output?.WriteLine("WaitForObservableToUpdate ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output?.WriteLine("WaitForLatchedObserverToUpdate @ " + Time.CurrentTimeMillis + " Starting wait");
            return Time.WaitUntil(() => observer.TickCount >= current + countToWait && Time.CurrentTimeMillis >= minTime, maxWaitTime);
        }

        protected static string BucketToString(long[] eventCounts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (HystrixEventType eventType in HystrixEventTypeHelper.Values)
            {
                if (eventCounts[(int)eventType] > 0)
                {
                    sb.Append(eventType).Append("->").Append(eventCounts[(int)eventType]).Append(", ");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}

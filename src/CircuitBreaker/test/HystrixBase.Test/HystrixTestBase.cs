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
using Steeltoe.Common.Util;
using System;
using System.Text;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public abstract class HystrixTestBase : IDisposable
{
    protected HystrixRequestContext context;

    protected HystrixTestBase()
    {
        Before();
    }

    public void Before()
    {
        context = HystrixRequestContext.InitializeContext();

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            context?.Dispose();
            context = null;

            HystrixThreadPoolFactory.Shutdown();
        }
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
        var updated = false;
        var number = numberOfUpdates;

        using (observable.Subscribe(item =>
               {
                   number--;
                   if (number <= 0)
                   {
                       updated = true;
                       output?.WriteLine("WaitForObservableToUpdate @ " + Time.CurrentTimeMillis + " : required updates received");
                   }

                   output?.WriteLine("WaitForObservableToUpdate @ " + Time.CurrentTimeMillis + " : " + item);
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
        var countToWait = count;

        output?.WriteLine("WaitForObservableToUpdate ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        output?.WriteLine("WaitForLatchedObserverToUpdate @ " + Time.CurrentTimeMillis + " Starting wait");
        return Time.WaitUntil(() => observer.TickCount >= current + countToWait, maxWaitTime);
    }

    public virtual bool WaitForLatchedObserverToUpdate<T>(TestObserverBase<T> observer, int count, int minWaitTime, int maxWaitTime, ITestOutputHelper output = null)
    {
        var current = observer.TickCount;
        var countToWait = count;
        var minTime = Time.CurrentTimeMillis + minWaitTime;

        output?.WriteLine("WaitForObservableToUpdate ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        output?.WriteLine("WaitForLatchedObserverToUpdate @ " + Time.CurrentTimeMillis + " Starting wait");
        return Time.WaitUntil(() => observer.TickCount >= current + countToWait && Time.CurrentTimeMillis >= minTime, maxWaitTime);
    }

    protected static string BucketToString(long[] eventCounts)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        foreach (var eventType in HystrixEventTypeHelper.Values)
        {
            if (eventCounts[(int)eventType] > 0)
            {
                sb.Append(eventType).Append("->").Append(eventCounts[(int)eventType]).Append(", ");
            }
        }

        sb.Append(']');
        return sb.ToString();
    }
}

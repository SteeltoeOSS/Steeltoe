// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.Test.EventSources;

public class HystrixEventSourceServiceTests : HystrixTestBase
{
    [Fact]
    public void Constructor_SetsupStream()
    {
        var stream = HystrixDashboardStream.GetInstance();
        var service = new HystrixEventSourceService(stream);
        Assert.NotNull(service.Stream);
    }

    [Fact]
    public void TestSubscription()
    {
        var stream = HystrixDashboardStream.GetInstance();
        var service = new HystrixEventSourceService(stream);

        using var listener = new HystrixEventsListener();

        service.OnNext(GetTestData());

        var i = 0;
        while (i++ < 100
               && listener.CommandEvents.Count <= 0
               && listener.ThreadPoolEvents.Count <= 0
               && listener.CollapserEvents.Count <= 0)
        {
            Thread.Sleep(1000);
        }

        Assert.True(listener.CommandEvents.Count > 0);
    }

    public HystrixDashboardStream.DashboardData GetTestData()
    {
        var commandKey = new HystrixCommandKeyDefault("command");
        var tpKey = new HystrixThreadPoolKeyDefault("threadPool");
        var collapserKey = new HystrixCollapserKeyDefault("collapser");

        var commandMetric = new HystrixCommandMetrics(
            commandKey,
            new HystrixCommandGroupKeyDefault("group"),
            tpKey,
            new HystrixCommandOptions(),
            HystrixEventNotifierDefault.GetInstance());
        var threadPoolMetric = HystrixThreadPoolMetrics.GetInstance(
            tpKey,
            new HystrixSyncTaskScheduler(new HystrixThreadPoolOptions()),
            new HystrixThreadPoolOptions());
        var commandMetrics = new List<HystrixCommandMetrics> { commandMetric };
        var collapserOptions = new HystrixCollapserOptions(collapserKey);
        var threadPoolMetrics = new List<HystrixThreadPoolMetrics> { threadPoolMetric };

        var collapserMetrics = new List<HystrixCollapserMetrics> { HystrixCollapserMetrics.GetInstance(collapserKey, collapserOptions) };
        return new HystrixDashboardStream.DashboardData(commandMetrics, threadPoolMetrics, collapserMetrics);
    }

    public class HystrixEventsListener : EventListener
    {
        private const string EventSourceName = "Steeltoe.Hystrix.Events";

        private enum EventTypes
        {
            CommandMetrics,
            ThreadPoolMetrics,
            CollapserMetrics
        }

        public List<EventWrittenEventArgs> CommandEvents = new ();
        public List<EventWrittenEventArgs> ThreadPoolEvents = new ();
        public List<EventWrittenEventArgs> CollapserEvents = new ();

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (Enum.TryParse<EventTypes>(eventData.EventName, out var eventType))
            {
                switch (eventType)
                {
                    case EventTypes.CommandMetrics:
                        CommandEvents.Add(eventData);
                        break;
                    case EventTypes.ThreadPoolMetrics:
                        ThreadPoolEvents.Add(eventData);
                        break;
                    case EventTypes.CollapserMetrics:
                        CollapserEvents.Add(eventData);
                        break;
                }
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == EventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }
    }
}

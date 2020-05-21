// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.Test.EventSources
{
    public class HystrixEventSourceServiceTests
    {
        [Fact]
        public void Constructor_SetsupStream()
        {
            var stream = HystrixDashboardStream.GetInstance();
            var service = new HystrixEventSourceService(stream);
            Assert.NotNull(service.Stream);
        }

        [Fact]
        public async void TestSubscription()
        {
            var stream = HystrixDashboardStream.GetInstance();
            var service = new HystrixEventSourceService(stream);

            using (var listener = new HystrixEventsListener())
            {
                await service.StartAsync(new CancellationTokenSource().Token);

                service.OnNext(GetTestData());

                int i = 0;
                while (i++ < 100
                    && listener.CommandEvents.Count <= 0
                    && listener.ThreadPoolEvents.Count <= 0
                    && listener.CollapserEvents.Count <= 0)
                {
                    Thread.Sleep(1000);
                }

                Assert.True(listener.CommandEvents.Count > 0);
            }
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
            var commandMetrics = new List<HystrixCommandMetrics>() { commandMetric };
            var collapserOptions = new HystrixCollapserOptions(collapserKey);
            var threadPoolMetrics = new List<HystrixThreadPoolMetrics>() { threadPoolMetric };

            var collapserMetrics = new List<HystrixCollapserMetrics>() { HystrixCollapserMetrics.GetInstance(collapserKey, collapserOptions) };
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

            public List<EventWrittenEventArgs> CommandEvents = new List<EventWrittenEventArgs>();
            public List<EventWrittenEventArgs> ThreadPoolEvents = new List<EventWrittenEventArgs>();
            public List<EventWrittenEventArgs> CollapserEvents = new List<EventWrittenEventArgs>();

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData == null)
                {
                    throw new ArgumentNullException(nameof(eventData));
                }

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
}

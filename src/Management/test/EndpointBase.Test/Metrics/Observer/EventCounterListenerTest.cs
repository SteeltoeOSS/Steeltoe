// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointBase.Test.Metrics;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    public class EventCounterListenerTest : BaseTest
    {
        [Fact]
        public void EventCounterListenerGetsMetricsTest()
        {
            var options = new MetricsEndpointOptions();
            var stats = new TestOpenTelemetryMetrics();
            var factory = stats.Factory;
            var processor = stats.Processor;
            using var listener = new EventCounterListener(stats);

            Task.Delay(2000).Wait();

            factory.CollectAllMetrics();

            var longMetrics = new string[]
            {
                "System.Runtime.alloc-rate",
                "System.Runtime.gen-2-gc-count",
                "System.Runtime.threadpool-completed-items-count",
                "System.Runtime.monitor-lock-contention-count",
                "System.Runtime.gen-1-gc-count",
                "System.Runtime.gen-0-gc-count",
                "System.Runtime.exception-count"
            };
            var doubleMetrics = new string[]
            {
                "System.Runtime.time-in-gc",
                "System.Runtime.threadpool-thread-count",
                "System.Runtime.gen-1-size",
                "System.Runtime.threadpool-queue-length",
                "System.Runtime.gen-2-size",
                "System.Runtime.gc-heap-size",
                "System.Runtime.assembly-count",
                "System.Runtime.gen-0-size",
                "System.Runtime.cpu-usage",
                "System.Runtime.active-timer-count",
                "System.Runtime.loh-size",
                "System.Runtime.working-set"
            };

            foreach (var metric in longMetrics)
            {
                var summary = processor.GetMetricByName<long>(metric);
                Assert.NotNull(summary);
                Assert.True(summary.Count > 0);
            }

            foreach (var metric in doubleMetrics)
            {
                var summary = processor.GetMetricByName<double>(metric);
                Assert.NotNull(summary);
                Assert.True(summary.Count > 0);
            }
        }
    }
}

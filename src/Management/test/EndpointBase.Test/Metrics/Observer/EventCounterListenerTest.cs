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

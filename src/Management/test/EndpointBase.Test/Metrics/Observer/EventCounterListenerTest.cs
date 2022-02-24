//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the Apache 2.0 License.
//// See the LICENSE file in the project root for more information.

//using Steeltoe.Management.Endpoint.Test;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Xunit;

//namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
//{
//    [System.Obsolete]
//    public class EventCounterListenerTest : BaseTest
//    {
//        private readonly string[] _longMetrics = new string[]
//            {
//                "System.Runtime.alloc-rate",
//                "System.Runtime.gen-2-gc-count",
//                "System.Runtime.threadpool-completed-items-count",
//                "System.Runtime.monitor-lock-contention-count",
//                "System.Runtime.gen-1-gc-count",
//                "System.Runtime.gen-0-gc-count",
//                "System.Runtime.exception-count"
//            };

//        private readonly string[] _doubleMetrics = new string[]
//            {
//                    "System.Runtime.time-in-gc",
//                    "System.Runtime.threadpool-thread-count",
//                    "System.Runtime.gen-1-size",
//                    "System.Runtime.threadpool-queue-length",
//                    "System.Runtime.gen-2-size",
//                    "System.Runtime.gc-heap-size",
//                    "System.Runtime.assembly-count",
//                    "System.Runtime.gen-0-size",
//                    "System.Runtime.cpu-usage",
//                    "System.Runtime.active-timer-count",
//                    "System.Runtime.loh-size",
//                    "System.Runtime.working-set"
//            };

//        [Fact]
//        public void EventCounterListenerGetsMetricsTest()
//        {
//            var options = new MetricsEndpointOptions();
//            using var listener = new EventCounterListener(new MetricsObserverOptions());

//            Task.Delay(2000).Wait();


//            foreach (var metric in _longMetrics)
//            {
//                var summary = processor.GetMetricByName<long>(metric);
//                Assert.NotNull(summary);
//                Assert.True(summary.Count > 0);
//            }

//            foreach (var metric in _doubleMetrics)
//            {
//                var summary = processor.GetMetricByName<double>(metric);
//                Assert.NotNull(summary);
//                Assert.True(summary.Count > 0);
//            }
//        }

//        [Fact]
//        public void EventCounterListenerGetsMetricsWithExclusionsTest()
//        {
//            var options = new MetricsEndpointOptions();
//            var stats = new TestOpenTelemetryMetrics();
//            var factory = stats.Factory;
//            var processor = stats.Processor;
//            var exclusions = new List<string> { "alloc-rate", "threadpool-completed-items-count", "gen-1-gc-count", "gen-1-size" };
//            using var listener = new EventCounterListener(stats, new MetricsObserverOptions { ExcludedMetrics = exclusions });

//            Task.Delay(2000).Wait();

//            factory.CollectAllMetrics();

//            foreach (var metric in _longMetrics)
//            {
//                var summary = processor.GetMetricByName<long>(metric);
//                if (!exclusions.Contains(metric.Replace("System.Runtime.", string.Empty)))
//                {
//                    Assert.NotNull(summary);
//                    Assert.True(summary.Count > 0);
//                }
//                else
//                {
//                    Assert.Null(summary);
//                }
//            }

//            foreach (var metric in _doubleMetrics)
//            {
//                var summary = processor.GetMetricByName<double>(metric);
//                if (!exclusions.Contains(metric.Replace("System.Runtime.", string.Empty)))
//                {
//                    Assert.NotNull(summary);
//                    Assert.True(summary.Count > 0);
//                }
//                else
//                {
//                    Assert.Null(summary);
//                }
//            }
//        }
//    }
//}

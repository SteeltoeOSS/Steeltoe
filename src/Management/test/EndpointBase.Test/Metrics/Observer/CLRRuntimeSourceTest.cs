// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointBase.Test.Metrics;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    [Obsolete]
    public class CLRRuntimeSourceTest : BaseTest
    {
        private const string DIAGNOSTIC_NAME = "Steeltoe.ClrMetrics";

        [Fact]
        public void Poll_GeneratesExpectedEvents()
        {
            var source = new CLRRuntimeSource();
            var listener = source.Source as DiagnosticListener;

            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new TestObserver(options, stats, null);

            listener.Subscribe(observer);

            source.Poll();

            Assert.Equal(2, observer.Events.Count);
            Assert.Equal(2, observer.Args.Count);

            Assert.Equal(CLRRuntimeSource.HEAP_EVENT, observer.Events[0]);
            Assert.Equal(CLRRuntimeSource.THREADS_EVENT, observer.Events[1]);

            var heapMetrics = (CLRRuntimeSource.HeapMetrics)observer.Args[0];
            Assert.NotEqual(0, heapMetrics.TotalMemory);
            Assert.NotNull(heapMetrics.CollectionCounts);
            Assert.NotEqual(0, heapMetrics.CollectionCounts.Count);

            var threadMetrics = (CLRRuntimeSource.ThreadMetrics)observer.Args[1];
            Assert.NotEqual(0, threadMetrics.AvailableThreadCompletionPort);
            Assert.NotEqual(0, threadMetrics.AvailableThreadPoolWorkers);
            Assert.NotEqual(0, threadMetrics.MaxThreadCompletionPort);
            Assert.NotEqual(0, threadMetrics.MaxThreadPoolWorkers);
        }

        private class TestObserver : MetricsObserver
        {
            public List<string> Events { get; set; } = new List<string>();

            public List<object> Args { get; set; } = new List<object>();

            public TestObserver(IMetricsObserverOptions options, IStats stats, ILogger logger)
                : base("TestObserver", DIAGNOSTIC_NAME, options, stats, logger)
            {
            }

            public override void ProcessEvent(string evnt, object arg)
            {
                Events.Add(evnt);
                Args.Add(arg);
            }
        }
    }
}

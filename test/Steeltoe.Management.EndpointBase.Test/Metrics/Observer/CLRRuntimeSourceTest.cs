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

using Microsoft.Extensions.Logging;
using OpenCensus.Stats;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    public class CLRRuntimeSourceTest : BaseTest
    {
        private const string DIAGNOSTIC_NAME = "Steeltoe.ClrMetrics";

        [Fact]
        public void Poll_GeneratesExpectedEvents()
        {
            var source = new CLRRuntimeSource();
            var listener = source.Source as DiagnosticListener;

            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new TestObserver(options, stats, tags, null);

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

            public TestObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger logger)
                : base("TestObserver", DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
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

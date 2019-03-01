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

using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    public class CLRRuntimeObserverTest : BaseTest
    {
        [Fact]
        public void Constructor_RegistersExpectedViews()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new CLRRuntimeObserver(options, stats, tags, null);

            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.memory.used")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.gc.collections")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.threadpool.active")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.threadpool.avail")));
        }

        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new CLRRuntimeObserver(options, stats, tags, null);

            observer.ProcessEvent("foobar", null);
            observer.ProcessEvent(CLRRuntimeObserver.HEAP_EVENT, null);
            observer.ProcessEvent(CLRRuntimeObserver.THREADS_EVENT, null);
        }

        [Fact]
        public void HandleHeapEvent_RecordsValues()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new CLRRuntimeObserver(options, stats, tags, null);

            CLRRuntimeSource.HeapMetrics metrics = new CLRRuntimeSource.HeapMetrics(1000, new List<long>() { 10, 20, 30 });
            observer.HandleHeapEvent(metrics);

            var memUsedViewData = stats.ViewManager.GetView(ViewName.Create("clr.memory.used"));
            var aggData = MetricsHelpers.SumWithTags(memUsedViewData) as IMeanData;
            Assert.Equal(1000, aggData.Mean);
            Assert.Equal(1000, aggData.Max);
            Assert.Equal(1000, aggData.Min);

            var gcViewData = stats.ViewManager.GetView(ViewName.Create("clr.gc.collections"));
            var aggData2 = MetricsHelpers.SumWithTags(gcViewData) as ISumDataLong;
            Assert.Equal(60, aggData2.Sum);

            aggData2 = MetricsHelpers.SumWithTags(gcViewData, new List<ITagValue>() { TagValue.Create("gen0") }) as ISumDataLong;
            Assert.Equal(10, aggData2.Sum);

            aggData2 = MetricsHelpers.SumWithTags(gcViewData, new List<ITagValue>() { TagValue.Create("gen1") }) as ISumDataLong;
            Assert.Equal(20, aggData2.Sum);

            aggData2 = MetricsHelpers.SumWithTags(gcViewData, new List<ITagValue>() { TagValue.Create("gen2") }) as ISumDataLong;
            Assert.Equal(30, aggData2.Sum);

            metrics = new CLRRuntimeSource.HeapMetrics(5000, new List<long>() { 15, 25, 30 });
            observer.HandleHeapEvent(metrics);

            memUsedViewData = stats.ViewManager.GetView(ViewName.Create("clr.memory.used"));
            aggData = MetricsHelpers.SumWithTags(memUsedViewData) as IMeanData;
            Assert.Equal((5000 + 1000) / 2, aggData.Mean);
            Assert.Equal(5000, aggData.Max);
            Assert.Equal(1000, aggData.Min);

            gcViewData = stats.ViewManager.GetView(ViewName.Create("clr.gc.collections"));
            aggData2 = MetricsHelpers.SumWithTags(gcViewData) as ISumDataLong;
            Assert.Equal(70, aggData2.Sum);

            aggData2 = MetricsHelpers.SumWithTags(gcViewData, new List<ITagValue>() { TagValue.Create("gen0") }) as ISumDataLong;
            Assert.Equal(15, aggData2.Sum);

            aggData2 = MetricsHelpers.SumWithTags(gcViewData, new List<ITagValue>() { TagValue.Create("gen1") }) as ISumDataLong;
            Assert.Equal(25, aggData2.Sum);

            aggData2 = MetricsHelpers.SumWithTags(gcViewData, new List<ITagValue>() { TagValue.Create("gen2") }) as ISumDataLong;
            Assert.Equal(30, aggData2.Sum);
        }

        [Fact]
        public void HandleThreadsEvent_RecordsValues()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new CLRRuntimeObserver(options, stats, tags, null);

            CLRRuntimeSource.ThreadMetrics metrics = new CLRRuntimeSource.ThreadMetrics(100, 100, 200, 200);
            observer.HandleThreadsEvent(metrics);

            var live = stats.ViewManager.GetView(ViewName.Create("clr.threadpool.active"));
            var aggData = MetricsHelpers.SumWithTags(live) as IMeanData;
            Assert.Equal(100, aggData.Mean);
            Assert.Equal(100, aggData.Min);
            Assert.Equal(100, aggData.Max);

            aggData = MetricsHelpers.SumWithTags(live, new List<ITagValue>() { TagValue.Create("worker") }) as IMeanData;
            Assert.Equal(100, aggData.Mean);
            Assert.Equal(100, aggData.Min);
            Assert.Equal(100, aggData.Max);

            aggData = MetricsHelpers.SumWithTags(live, new List<ITagValue>() { TagValue.Create("completionPort") }) as IMeanData;
            Assert.Equal(100, aggData.Mean);
            Assert.Equal(100, aggData.Min);
            Assert.Equal(100, aggData.Max);

            var avail = stats.ViewManager.GetView(ViewName.Create("clr.threadpool.avail"));
            aggData = MetricsHelpers.SumWithTags(avail) as IMeanData;
            Assert.Equal(100, aggData.Mean);
            Assert.Equal(100, aggData.Min);
            Assert.Equal(100, aggData.Max);

            aggData = MetricsHelpers.SumWithTags(avail, new List<ITagValue>() { TagValue.Create("worker") }) as IMeanData;
            Assert.Equal(100, aggData.Mean);
            Assert.Equal(100, aggData.Min);
            Assert.Equal(100, aggData.Max);

            aggData = MetricsHelpers.SumWithTags(avail, new List<ITagValue>() { TagValue.Create("completionPort") }) as IMeanData;
            Assert.Equal(100, aggData.Mean);
            Assert.Equal(100, aggData.Min);
            Assert.Equal(100, aggData.Max);
        }
    }
}

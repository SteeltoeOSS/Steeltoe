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

using Microsoft.Extensions.Logging;
using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class CLRRuntimeObserver : MetricsObserver
    {
        internal const string OBSERVER_NAME = "CLRRuntimeObserver";
        internal const string DIAGNOSTIC_NAME = "Steeltoe.ClrMetrics";

        internal const string HEAP_EVENT = "Steeltoe.ClrMetrics.Heap";
        internal const string THREADS_EVENT = "Steeltoe.ClrMetrics.Threads";

        private const string GENERATION_TAGVALUE_NAME = "gen";

        private readonly ITagKey threadKindKey = TagKey.Create("kind");
        private readonly ITagValue threadPoolWorkerKind = TagValue.Create("worker");
        private readonly ITagValue threadPoolComppKind = TagValue.Create("completionPort");

        private readonly IMeasureLong activeThreadsMeasure;
        private readonly IMeasureLong availThreadsMeasure;
        private readonly ITagContext threadPoolWorkerTagValues;
        private readonly ITagContext threadPoolCompPortTagValues;

        private readonly ITagKey generationKey = TagKey.Create("generation");
        private readonly IMeasureLong collectionCountMeasure;

        private readonly ITagKey memoryAreaKey = TagKey.Create("area");
        private readonly ITagValue heapArea = TagValue.Create("heap");
        private readonly ITagValue nonHeapArea = TagValue.Create("nonheap");
        private readonly IMeasureLong memoryUsedMeasure;
        private readonly ITagContext memoryTagValues;

        private CLRRuntimeSource.HeapMetrics previous = default(CLRRuntimeSource.HeapMetrics);

        public CLRRuntimeObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<CLRRuntimeObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            memoryUsedMeasure = MeasureLong.Create("memory.used.value", "Current CLR memory usage", MeasureUnit.Bytes);
            collectionCountMeasure = MeasureLong.Create("collection.count", "Garbage collection count", "count");
            activeThreadsMeasure = MeasureLong.Create("active.thread.value", "Active thread count", "count");
            availThreadsMeasure = MeasureLong.Create("avail.thread.value", "Available thread count", "count");

            memoryTagValues = Tagger.CurrentBuilder.Put(memoryAreaKey, heapArea).Build();
            threadPoolWorkerTagValues = Tagger.CurrentBuilder.Put(threadKindKey, threadPoolWorkerKind).Build();
            threadPoolCompPortTagValues = Tagger.CurrentBuilder.Put(threadKindKey, threadPoolComppKind).Build();

            RegisterViews();
        }

        public override void ProcessEvent(string evnt, object arg)
        {
            if (arg == null)
            {
                return;
            }

            if (evnt == HEAP_EVENT)
            {
                Logger?.LogTrace("HandleHeapEvent start {thread}", Thread.CurrentThread.ManagedThreadId);
                var metrics = (CLRRuntimeSource.HeapMetrics)arg;
                HandleHeapEvent(metrics);
                Logger?.LogTrace("HandleHeapEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == THREADS_EVENT)
            {
                Logger?.LogTrace("HandleThreadsEvent start {thread}", Thread.CurrentThread.ManagedThreadId);
                var metrics = (CLRRuntimeSource.ThreadMetrics)arg;
                HandleThreadsEvent(metrics);
                Logger?.LogTrace("HandleThreadsEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleHeapEvent(CLRRuntimeSource.HeapMetrics metrics)
        {
            StatsRecorder
                .NewMeasureMap()
                .Put(memoryUsedMeasure, metrics.TotalMemory)
                .Record(memoryTagValues);

            for (int i = 0; i < metrics.CollectionCounts.Count; i++)
            {
                var count = metrics.CollectionCounts[i];
                if (previous.CollectionCounts != null && i < previous.CollectionCounts.Count)
                {
                    if (previous.CollectionCounts[i] <= count)
                    {
                        count = count - previous.CollectionCounts[i];
                    }
                }

                var tagContext = Tagger
                    .EmptyBuilder
                    .Put(generationKey, TagValue.Create(GENERATION_TAGVALUE_NAME + i.ToString()))
                    .Build();

                StatsRecorder
                    .NewMeasureMap()
                    .Put(collectionCountMeasure, count)
                    .Record(tagContext);
            }

            previous = metrics;
        }

        protected internal void HandleThreadsEvent(CLRRuntimeSource.ThreadMetrics metrics)
        {
            var activeWorkers = metrics.MaxThreadPoolWorkers - metrics.AvailableThreadPoolWorkers;
            var activeCompPort = metrics.MaxThreadCompletionPort - metrics.AvailableThreadCompletionPort;

            StatsRecorder
                .NewMeasureMap()
                .Put(activeThreadsMeasure, activeWorkers)
                .Put(availThreadsMeasure, metrics.AvailableThreadPoolWorkers)
                .Record(threadPoolWorkerTagValues);

            StatsRecorder
                .NewMeasureMap()
                .Put(activeThreadsMeasure, activeCompPort)
                .Put(availThreadsMeasure, metrics.AvailableThreadCompletionPort)
                .Record(threadPoolCompPortTagValues);
        }

        protected internal void RegisterViews()
        {
            IView view = View.Create(
                    ViewName.Create("clr.memory.used"),
                    "Current CLR memory usage",
                    memoryUsedMeasure,
                    Mean.Create(),
                    new List<ITagKey>() { memoryAreaKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("clr.gc.collections"),
                    "Garbage collection count",
                    collectionCountMeasure,
                    Sum.Create(),
                    new List<ITagKey>() { generationKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("clr.threadpool.active"),
                    "Active thread count",
                    activeThreadsMeasure,
                    Mean.Create(),
                    new List<ITagKey>() { threadKindKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("clr.threadpool.avail"),
                    "Available thread count",
                    availThreadsMeasure,
                    Mean.Create(),
                    new List<ITagKey>() { threadKindKey });
            ViewManager.RegisterView(view);
        }
    }
}

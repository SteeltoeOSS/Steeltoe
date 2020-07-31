// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        private readonly ITagKey _threadKindKey = TagKey.Create("kind");
        private readonly ITagValue _threadPoolWorkerKind = TagValue.Create("worker");
        private readonly ITagValue _threadPoolComppKind = TagValue.Create("completionPort");

        private readonly IMeasureLong _activeThreadsMeasure;
        private readonly IMeasureLong _availThreadsMeasure;
        private readonly ITagContext _threadPoolWorkerTagValues;
        private readonly ITagContext _threadPoolCompPortTagValues;

        private readonly ITagKey _generationKey = TagKey.Create("generation");
        private readonly IMeasureLong _collectionCountMeasure;

        private readonly ITagKey _memoryAreaKey = TagKey.Create("area");
        private readonly ITagValue _heapArea = TagValue.Create("heap");
        private readonly IMeasureLong _memoryUsedMeasure;
        private readonly ITagContext _memoryTagValues;

        private CLRRuntimeSource.HeapMetrics _previous = default;

        public CLRRuntimeObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<CLRRuntimeObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            _memoryUsedMeasure = MeasureLong.Create("memory.used.value", "Current CLR memory usage", MeasureUnit.Bytes);
            _collectionCountMeasure = MeasureLong.Create("collection.count", "Garbage collection count", "count");
            _activeThreadsMeasure = MeasureLong.Create("active.thread.value", "Active thread count", "count");
            _availThreadsMeasure = MeasureLong.Create("avail.thread.value", "Available thread count", "count");

            _memoryTagValues = Tagger.CurrentBuilder.Put(_memoryAreaKey, _heapArea).Build();
            _threadPoolWorkerTagValues = Tagger.CurrentBuilder.Put(_threadKindKey, _threadPoolWorkerKind).Build();
            _threadPoolCompPortTagValues = Tagger.CurrentBuilder.Put(_threadKindKey, _threadPoolComppKind).Build();

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
                .Put(_memoryUsedMeasure, metrics.TotalMemory)
                .Record(_memoryTagValues);

            for (var i = 0; i < metrics.CollectionCounts.Count; i++)
            {
                var count = metrics.CollectionCounts[i];
                if (_previous.CollectionCounts != null && i < _previous.CollectionCounts.Count && _previous.CollectionCounts[i] <= count)
                {
                    count -= _previous.CollectionCounts[i];
                }

                var tagContext = Tagger
                    .EmptyBuilder
                    .Put(_generationKey, TagValue.Create(GENERATION_TAGVALUE_NAME + i.ToString()))
                    .Build();

                StatsRecorder
                    .NewMeasureMap()
                    .Put(_collectionCountMeasure, count)
                    .Record(tagContext);
            }

            _previous = metrics;
        }

        protected internal void HandleThreadsEvent(CLRRuntimeSource.ThreadMetrics metrics)
        {
            var activeWorkers = metrics.MaxThreadPoolWorkers - metrics.AvailableThreadPoolWorkers;
            var activeCompPort = metrics.MaxThreadCompletionPort - metrics.AvailableThreadCompletionPort;

            StatsRecorder
                .NewMeasureMap()
                .Put(_activeThreadsMeasure, activeWorkers)
                .Put(_availThreadsMeasure, metrics.AvailableThreadPoolWorkers)
                .Record(_threadPoolWorkerTagValues);

            StatsRecorder
                .NewMeasureMap()
                .Put(_activeThreadsMeasure, activeCompPort)
                .Put(_availThreadsMeasure, metrics.AvailableThreadCompletionPort)
                .Record(_threadPoolCompPortTagValues);
        }

        protected internal void RegisterViews()
        {
            var view = View.Create(
                    ViewName.Create("clr.memory.used"),
                    "Current CLR memory usage",
                    _memoryUsedMeasure,
                    Mean.Create(),
                    new List<ITagKey>() { _memoryAreaKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("clr.gc.collections"),
                    "Garbage collection count",
                    _collectionCountMeasure,
                    Sum.Create(),
                    new List<ITagKey>() { _generationKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("clr.threadpool.active"),
                    "Active thread count",
                    _activeThreadsMeasure,
                    Mean.Create(),
                    new List<ITagKey>() { _threadKindKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("clr.threadpool.avail"),
                    "Available thread count",
                    _availThreadsMeasure,
                    Mean.Create(),
                    new List<ITagKey>() { _threadKindKey });
            ViewManager.RegisterView(view);
        }
    }
}

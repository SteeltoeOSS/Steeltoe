// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    [Obsolete("Use EventListeners instead")]
    public class CLRRuntimeObserver : MetricsObserver
    {
        internal const string OBSERVER_NAME = "CLRRuntimeObserver";
        internal const string DIAGNOSTIC_NAME = "Steeltoe.ClrMetrics";

        internal const string HEAP_EVENT = "Steeltoe.ClrMetrics.Heap";
        internal const string THREADS_EVENT = "Steeltoe.ClrMetrics.Threads";

        private const string GENERATION_TAGVALUE_NAME = "gen";
        private readonly MeasureMetric<long> _activeThreads;
        private readonly MeasureMetric<long> _availThreads;
        private readonly string _generationKey = "generation";
        private readonly MeasureMetric<long> _collectionCount;
        private readonly MeasureMetric<long> _memoryUsed;

        private readonly IEnumerable<KeyValuePair<string, string>> _memoryLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("area", "heap") };
        private readonly IEnumerable<KeyValuePair<string, string>> _threadPoolWorkerLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("kind", "worker") };
        private readonly IEnumerable<KeyValuePair<string, string>> _threadPoolComPortLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("kind", "completionPort") };

        private CLRRuntimeSource.HeapMetrics _previous = default;

        public CLRRuntimeObserver(IMetricsObserverOptions options, IStats stats, ILogger<CLRRuntimeObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, stats, logger)
        {
            _memoryUsed = Meter.CreateInt64Measure("clr.memory.used");
            _collectionCount = Meter.CreateInt64Measure("clr.gc.collections");
            _activeThreads = Meter.CreateInt64Measure("clr.threadpool.active");
            _availThreads = Meter.CreateInt64Measure("clr.threadpool.avail");

            // TODO: Pending View API
            // memoryTagValues = Tagger.CurrentBuilder.Put(memoryAreaKey, heapArea).Build();
            // threadPoolWorkerTagValues = Tagger.CurrentBuilder.Put(threadKindKey, threadPoolWorkerKind).Build();
            // threadPoolCompPortTagValues = Tagger.CurrentBuilder.Put(threadKindKey, threadPoolComppKind).Build();

            // RegisterViews();
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
            var context = default(SpanContext);
            _memoryUsed.Record(context, metrics.TotalMemory, _memoryLabels);
            for (int i = 0; i < metrics.CollectionCounts.Count; i++)
            {
                var count = metrics.CollectionCounts[i];
                if (_previous.CollectionCounts != null && i < _previous.CollectionCounts.Count && _previous.CollectionCounts[i] <= count)
                {
                    count -= _previous.CollectionCounts[i];
                }

                var genKeylabelSet = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(_generationKey, GENERATION_TAGVALUE_NAME + i.ToString()) };
                _collectionCount.Record(context, count, genKeylabelSet);
            }

            _previous = metrics;
        }

        protected internal void HandleThreadsEvent(CLRRuntimeSource.ThreadMetrics metrics)
        {
            var activeWorkers = metrics.MaxThreadPoolWorkers - metrics.AvailableThreadPoolWorkers;
            var activeCompPort = metrics.MaxThreadCompletionPort - metrics.AvailableThreadCompletionPort;

            _activeThreads.Record(default(SpanContext), activeWorkers, _threadPoolWorkerLabels);
            _availThreads.Record(default(SpanContext), metrics.AvailableThreadPoolWorkers, _threadPoolWorkerLabels);

            _activeThreads.Record(default(SpanContext), activeCompPort, _threadPoolComPortLabels);
            _availThreads.Record(default(SpanContext), metrics.AvailableThreadCompletionPort, _threadPoolComPortLabels);
        }

        // TODO: Pending View API
        /*        protected internal void RegisterViews()
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
                }*/
    }
}

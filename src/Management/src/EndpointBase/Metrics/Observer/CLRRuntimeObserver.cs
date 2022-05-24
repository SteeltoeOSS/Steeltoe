// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class CLRRuntimeObserver : IRuntimeDiagnosticSource
    {
        internal const string OBSERVER_NAME = "CLRRuntimeObserver";
        internal const string DIAGNOSTIC_NAME = "Steeltoe.ClrMetrics";

        internal const string HEAP_EVENT = "Steeltoe.ClrMetrics.Heap";
        internal const string THREADS_EVENT = "Steeltoe.ClrMetrics.Threads";

        private const string GENERATION_TAGVALUE_NAME = "gen";
        private const string GENERATION_KEY = "generation";

        private readonly Dictionary<string, object> _heapTags = new () { { "area", "heap" } };
        private readonly Dictionary<string, object> _workerTags = new () { { "kind", "worker" } };
        private readonly Dictionary<string, object> _comPortTags = new () { { "kind", "completionPort" } };

        private ObservableGauge<double> _memoryUsedMeasure;
        private ObservableGauge<long> _collectionCountMeasure;
        private ObservableGauge<long> _activeThreadsMeasure;
        private ObservableGauge<long> _availThreadsMeasure;
        private ObservableGauge<double> _processUptimeMeasure;

        private CLRRuntimeSource.HeapMetrics _previous = default;

        public CLRRuntimeObserver(IViewRegistry viewRegistry, ILogger<CLRRuntimeObserver> logger)
        {
            var meter = OpenTelemetryMetrics.Meter;
            _memoryUsedMeasure = meter.CreateObservableGauge("clr.memory.used", GetMemoryUsed, "Current CLR memory usage", "bytes");
            _collectionCountMeasure = meter.CreateObservableGauge("clr.gc.collections", GetCollectionCount, "Garbage collection count", "count");

            _activeThreadsMeasure = meter.CreateObservableGauge("clr.threadpool.active", GetActiveThreadPoolWorkers, "Active thread count", "count");
            _availThreadsMeasure = meter.CreateObservableGauge("clr.threadpool.avail", GetAvailableThreadPoolWorkers, "Available thread count", "count");

            _processUptimeMeasure = meter.CreateObservableGauge("clr.process.uptime", GetUptime, "Process uptime in seconds", "count");
            RegisterViews(viewRegistry);
        }

        protected internal void RegisterViews(IViewRegistry viewRegistry)
        {
            // Currently Api does not support changing aggregations via views

            // IView view = View.Create(
            //        ViewName.Create("clr.memory.used"),
            //        "Current CLR memory usage",
            //        _memoryUsedMeasure,
            //        Mean.Create(),
            //        new List<ITagKey>() { memoryAreaKey });
            // ViewManager.RegisterView(view);

            // view = View.Create(
            //        ViewName.Create("clr.gc.collections"),
            //        "Garbage collection count",
            //        collectionCountMeasure,
            //        Sum.Create(),
            //        new List<ITagKey>() { generationKey });
            // ViewManager.RegisterView(view);

            // view = View.Create(
            //        ViewName.Create("clr.threadpool.active"),
            //        "Active thread count",
            //        activeThreadsMeasure,
            //        Mean.Create(),
            //        new List<ITagKey>() { threadKindKey });
            // ViewManager.RegisterView(view);

            // view = View.Create(
            //        ViewName.Create("clr.threadpool.avail"),
            //        "Available thread count",
            //        availThreadsMeasure,
            //        Mean.Create(),
            //        new List<ITagKey>() { threadKindKey });
            // ViewManager.RegisterView(view);
        }

        private IEnumerable<Measurement<long>> GetCollectionCount()
        {
            var metrics = CLRRuntimeSource.GetHeapMetrics();

            for (int i = 0; i < metrics.CollectionCounts.Count; i++)
            {
                var count = metrics.CollectionCounts[i];
                if (_previous.CollectionCounts != null && i < _previous.CollectionCounts.Count && _previous.CollectionCounts[i] <= count)
                {
                    count = count - _previous.CollectionCounts[i];
                }

                var tags = new Dictionary<string, object> { { GENERATION_KEY, GENERATION_TAGVALUE_NAME + i } };

                yield return new Measurement<long>(count, tags.AsReadonlySpan());
            }
        }

        private Measurement<double> GetMemoryUsed()
        {
            var metrics = CLRRuntimeSource.GetHeapMetrics();
            return new Measurement<double>(metrics.TotalMemory, _heapTags.AsReadonlySpan());
        }

        private double GetUptime()
        {
            var diff = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return diff.TotalSeconds;
        }

        private IEnumerable<Measurement<long>> GetActiveThreadPoolWorkers()
        {
            var metrics = CLRRuntimeSource.GetThreadMetrics();
            var active = metrics.MaxThreadPoolWorkers - metrics.AvailableThreadPoolWorkers;
            var activeCompPort = metrics.MaxThreadCompletionPort - metrics.AvailableThreadCompletionPort;

            yield return new Measurement<long>(active, _workerTags.AsReadonlySpan());
            yield return new Measurement<long>(activeCompPort, _comPortTags.AsReadonlySpan());
        }

        private IEnumerable<Measurement<long>> GetAvailableThreadPoolWorkers()
        {
            var metrics = CLRRuntimeSource.GetThreadMetrics();
            yield return new Measurement<long>(metrics.AvailableThreadPoolWorkers, _workerTags.AsReadonlySpan());
            yield return new Measurement<long>(metrics.AvailableThreadCompletionPort, _comPortTags.AsReadonlySpan());
        }
    }
}
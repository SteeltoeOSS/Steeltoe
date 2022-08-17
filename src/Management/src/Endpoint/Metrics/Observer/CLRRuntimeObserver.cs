// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class ClrRuntimeObserver : IRuntimeDiagnosticSource
{
    private const string GenerationTagValueName = "gen";
    private const string GenerationKey = "generation";
    internal const string ObserverName = "CLRRuntimeObserver";
    internal const string DiagnosticName = "Steeltoe.ClrMetrics";

    internal const string HeapEvent = "Steeltoe.ClrMetrics.Heap";
    internal const string ThreadsEvent = "Steeltoe.ClrMetrics.Threads";

    private readonly Dictionary<string, object> _heapTags = new()
    {
        { "area", "heap" }
    };

    private readonly Dictionary<string, object> _workerTags = new()
    {
        { "kind", "worker" }
    };

    private readonly Dictionary<string, object> _comPortTags = new()
    {
        { "kind", "completionPort" }
    };

    private readonly ObservableGauge<double> _memoryUsedMeasure;
    private readonly ObservableGauge<long> _collectionCountMeasure;
    private readonly ObservableGauge<long> _activeThreadsMeasure;
    private readonly ObservableGauge<long> _availThreadsMeasure;
    private readonly ObservableGauge<double> _processUpTimeMeasure;

    private readonly ClrRuntimeSource.HeapMetrics _previous = default;

    public ClrRuntimeObserver(IViewRegistry viewRegistry)
    {
        Meter meter = OpenTelemetryMetrics.Meter;
        _memoryUsedMeasure = meter.CreateObservableGauge("clr.memory.used", GetMemoryUsed, "Current CLR memory usage", "bytes");
        _collectionCountMeasure = meter.CreateObservableGauge("clr.gc.collections", GetCollectionCount, "Garbage collection count", "count");

        _activeThreadsMeasure = meter.CreateObservableGauge("clr.threadpool.active", GetActiveThreadPoolWorkers, "Active thread count", "count");
        _availThreadsMeasure = meter.CreateObservableGauge("clr.threadpool.avail", GetAvailableThreadPoolWorkers, "Available thread count", "count");

        _processUpTimeMeasure = meter.CreateObservableGauge("clr.process.uptime", GetUpTime, "Process uptime in seconds", "count");
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
        ClrRuntimeSource.HeapMetrics metrics = ClrRuntimeSource.GetHeapMetrics();

        for (int i = 0; i < metrics.CollectionCounts.Count; i++)
        {
            long count = metrics.CollectionCounts[i];

            if (_previous.CollectionCounts != null && i < _previous.CollectionCounts.Count && _previous.CollectionCounts[i] <= count)
            {
                count = count - _previous.CollectionCounts[i];
            }

            var tags = new Dictionary<string, object>
            {
                { GenerationKey, GenerationTagValueName + i }
            };

            yield return new Measurement<long>(count, tags.AsReadonlySpan());
        }
    }

    private Measurement<double> GetMemoryUsed()
    {
        ClrRuntimeSource.HeapMetrics metrics = ClrRuntimeSource.GetHeapMetrics();
        return new Measurement<double>(metrics.TotalMemory, _heapTags.AsReadonlySpan());
    }

    private double GetUpTime()
    {
        TimeSpan diff = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return diff.TotalSeconds;
    }

    private IEnumerable<Measurement<long>> GetActiveThreadPoolWorkers()
    {
        ClrRuntimeSource.ThreadMetrics metrics = ClrRuntimeSource.GetThreadMetrics();
        long active = metrics.MaxThreadPoolWorkers - metrics.AvailableThreadPoolWorkers;
        long activeCompPort = metrics.MaxThreadCompletionPort - metrics.AvailableThreadCompletionPort;

        yield return new Measurement<long>(active, _workerTags.AsReadonlySpan());
        yield return new Measurement<long>(activeCompPort, _comPortTags.AsReadonlySpan());
    }

    private IEnumerable<Measurement<long>> GetAvailableThreadPoolWorkers()
    {
        ClrRuntimeSource.ThreadMetrics metrics = ClrRuntimeSource.GetThreadMetrics();
        yield return new Measurement<long>(metrics.AvailableThreadPoolWorkers, _workerTags.AsReadonlySpan());
        yield return new Measurement<long>(metrics.AvailableThreadCompletionPort, _comPortTags.AsReadonlySpan());
    }
}

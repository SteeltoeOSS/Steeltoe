// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.MetricCollectors.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

internal sealed class ClrRuntimeObserver : IRuntimeDiagnosticSource
{
    private const string GenerationTagValueName = "gen";
    private const string GenerationKey = "generation";

    private readonly Dictionary<string, object> _heapTags = new()
    {
        { "area", "heap" }
    };

    private readonly Dictionary<string, object> _workerTags = new()
    {
        { "kind", "worker" }
    };

    private readonly Dictionary<string, object> _completionPortTags = new()
    {
        { "kind", "completionPort" }
    };

    private readonly ClrRuntimeSource.HeapMetrics _previous = default;
    private readonly IOptionsMonitor<MetricsObserverOptions> _options;

    public ClrRuntimeObserver(IOptionsMonitor<MetricsObserverOptions> options)
    {
        ArgumentGuard.NotNull(options);

        _options = options;
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
        yield return new Measurement<long>(activeCompPort, _completionPortTags.AsReadonlySpan());
    }

    private IEnumerable<Measurement<long>> GetAvailableThreadPoolWorkers()
    {
        ClrRuntimeSource.ThreadMetrics metrics = ClrRuntimeSource.GetThreadMetrics();
        yield return new Measurement<long>(metrics.AvailableThreadPoolWorkers, _workerTags.AsReadonlySpan());
        yield return new Measurement<long>(metrics.AvailableThreadCompletionPort, _completionPortTags.AsReadonlySpan());
    }

    public void AddInstrumentation()
    {
        Meter meter = SteeltoeMetrics.Meter;

        if (_options.CurrentValue.GCEvents)
        {
            meter.CreateObservableGauge("clr.memory.used", GetMemoryUsed, "Current CLR memory usage", "bytes");
            meter.CreateObservableGauge("clr.gc.collections", GetCollectionCount, "Garbage collection count", "count");
            meter.CreateObservableGauge("clr.process.uptime", GetUpTime, "Process uptime in seconds", "count");
            meter.CreateObservableGauge("clr.cpu.count", () => System.Environment.ProcessorCount, "Total processor count", "count");
        }

        if (_options.CurrentValue.ThreadPoolEvents)
        {
            meter.CreateObservableGauge("clr.threadpool.active", GetActiveThreadPoolWorkers, "Active thread count", "count");
            meter.CreateObservableGauge("clr.threadpool.avail", GetAvailableThreadPoolWorkers, "Available thread count", "count");
        }
    }
}

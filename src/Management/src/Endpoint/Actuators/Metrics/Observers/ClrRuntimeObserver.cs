// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics.Observers;

internal sealed class ClrRuntimeObserver : IRuntimeDiagnosticSource
{
    private const string GenerationTagValueName = "gen";
    private const string GenerationKey = "generation";

    private readonly Dictionary<string, object?> _heapTags = new()
    {
        { "area", "heap" }
    };

    private readonly Dictionary<string, object?> _workerTags = new()
    {
        { "kind", "worker" }
    };

    private readonly Dictionary<string, object?> _completionPortTags = new()
    {
        { "kind", "completionPort" }
    };

    private readonly IOptionsMonitor<MetricsObserverOptions> _options;

    private ClrRuntimeSource.HeapMetrics? _previous;

    public ClrRuntimeObserver(IOptionsMonitor<MetricsObserverOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    private IEnumerable<Measurement<long>> GetCollectionCount()
    {
        ClrRuntimeSource.HeapMetrics metrics = ClrRuntimeSource.GetHeapMetrics();

        for (int index = 0; index < metrics.CollectionCounts.Count; index++)
        {
            long count = metrics.CollectionCounts[index];

            if (_previous != null && index < _previous.Value.CollectionCounts.Count && _previous.Value.CollectionCounts[index] <= count)
            {
                count -= _previous.Value.CollectionCounts[index];
            }

            var tags = new Dictionary<string, object?>
            {
                { GenerationKey, GenerationTagValueName + index }
            };

            yield return new Measurement<long>(count, tags.AsReadonlySpan());
            _previous = metrics;
        }
    }

    private Measurement<double> GetMemoryUsed()
    {
        ClrRuntimeSource.HeapMetrics metrics = ClrRuntimeSource.GetHeapMetrics();
        return new Measurement<double>(metrics.TotalMemory, _heapTags.AsReadonlySpan());
    }

    private double GetUpTime()
    {
        using var process = Process.GetCurrentProcess();
        TimeSpan diff = DateTime.UtcNow - process.StartTime.ToUniversalTime();
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

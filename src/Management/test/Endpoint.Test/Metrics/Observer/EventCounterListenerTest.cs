// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.Endpoint.Metrics.SystemDiagnosticsMetrics;

namespace Steeltoe.Management.Endpoint.Test.Metrics.Observer;

public sealed class EventCounterListenerTest : BaseTest
{
    private readonly MetricsExporterOptions _exporterOptions = new()
    {
        CacheDurationMilliseconds = 500
    };

    private readonly string[] _metrics =
    {
        "System.Runtime.alloc-rate",
        "System.Runtime.gen-2-gc-count",
        "System.Runtime.threadpool-completed-items-count",
        "System.Runtime.monitor-lock-contention-count",
        "System.Runtime.gen-1-gc-count",
        "System.Runtime.gen-0-gc-count",
        "System.Runtime.exception-count",
        "System.Runtime.time-in-gc",
        "System.Runtime.threadpool-thread-count",
        "System.Runtime.gen-1-size",
        "System.Runtime.threadpool-queue-length",
        "System.Runtime.gen-2-size",
        "System.Runtime.gc-heap-size",
        "System.Runtime.assembly-count",
        "System.Runtime.gen-0-size",
        "System.Runtime.cpu-usage",
        "System.Runtime.active-timer-count",
        "System.Runtime.loh-size",
        "System.Runtime.working-set"
    };

    [Fact]
    public async Task EventCounterListenerGetsMetricsTest()
    {
        var options = new MetricsObserverOptions
        {
            EventCounterEvents = true,
            EventCounterIntervalSec = 1
        };

        var optionsMonitor = new TestOptionsMonitor<MetricsObserverOptions>(options);

        using var listener = new EventCounterListener(optionsMonitor, NullLogger<EventCounterListener>.Instance);
        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var exporter = new MetricsExporter(_exporterOptions);
        using AggregationManager aggregationManager = GetTestMetrics(exporter);
        aggregationManager.Start();
        await Task.Delay(2000);

        (MetricsCollection<IList<MetricSample>> metricSamples, _) = exporter.Export();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, IList<MetricSample>>> summary = metricSamples.Where(pair => pair.Key == metric).ToList();
            Assert.True(summary != null, $"Summary was null for {metric}");
            Assert.True(summary.Count > 0, $"Summary was empty for {metric}");
        }
    }

    [Fact]
    public async Task EventCounterListenerGetsMetricsWithExclusionsTest()
    {
        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var exclusions = new List<string>
        {
            "alloc-rate",
            "threadpool-completed-items-count",
            "gen-1-gc-count",
            "gen-1-size"
        };

        var options = new MetricsObserverOptions
        {
            ExcludedMetrics = exclusions,
            EventCounterEvents = true,
            EventCounterIntervalSec = 1
        };

        var optionsMonitor = new TestOptionsMonitor<MetricsObserverOptions>(options);
        using var listener = new EventCounterListener(optionsMonitor, NullLogger<EventCounterListener>.Instance);

        var exporter = new MetricsExporter(_exporterOptions);
        using AggregationManager aggregationManager = GetTestMetrics(exporter);
        aggregationManager.Start();
        await Task.Delay(2000);

        (MetricsCollection<IList<MetricSample>> metricSamples, _) = exporter.Export();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, IList<MetricSample>>> summary = metricSamples.Where(pair => pair.Key == metric).ToList();

            if (!exclusions.Contains(metric.Replace("System.Runtime.", string.Empty, StringComparison.Ordinal)))
            {
                Assert.True(summary.Count > 0, $"Expected metrics for {metric}");
            }
            else
            {
                Assert.True(summary.Count == 0, $"Expected no metrics for {metric}");
            }
        }
    }

    [Fact]
    public async Task EventCounterListenerGetsMetricsWithInclusionsTest()
    {
        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var inclusions = new List<string>
        {
            "cpu-usage"
        };

        var optionsMonitor = new TestOptionsMonitor<MetricsObserverOptions>(new MetricsObserverOptions
        {
            IncludedMetrics = inclusions,
            EventCounterEvents = true,
            EventCounterIntervalSec = 1
        });

        using var listener = new EventCounterListener(optionsMonitor, NullLogger<EventCounterListener>.Instance);

        var exporter = new MetricsExporter(_exporterOptions);
        using AggregationManager aggregationManager = GetTestMetrics(exporter);
        aggregationManager.Start();
        await Task.Delay(2000);

        (MetricsCollection<IList<MetricSample>> metricSamples, _) = exporter.Export();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, IList<MetricSample>>> summary = metricSamples.Where(pair => pair.Key == metric).ToList();

            if (inclusions.Contains(metric.Substring("System.Runtime.".Length)))
            {
                Assert.True(summary.Count > 0, $"Expected metrics for {metric}");
            }
            else
            {
                Assert.True(summary.Count == 0, $"Expected no metrics for {metric}");
            }
        }
    }
}

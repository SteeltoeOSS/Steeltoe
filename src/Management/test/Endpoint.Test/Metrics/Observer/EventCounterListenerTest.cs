// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.MetricCollectors;
using Steeltoe.Management.MetricCollectors.Exporters;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics.Observer;

public class EventCounterListenerTest : BaseTest
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
        using var listener = new EventCounterListener(new MetricsObserverOptions());
        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var exporter = new SteeltoeExporter(_exporterOptions);
        AggregationManager aggregationManager = GetTestMetrics(exporter);
        aggregationManager.Start();
        await Task.Delay(2000);

        (MetricsCollection<List<MetricSample>> metricSamples, _) = exporter.Export();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = metricSamples.Where(x => x.Key == metric).ToList();
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
            ExcludedMetrics = exclusions
        };

        using var listener = new EventCounterListener(options);

        var exporter = new SteeltoeExporter(_exporterOptions);
        AggregationManager aggregationManager = GetTestMetrics(exporter);
        aggregationManager.Start();
        await Task.Delay(2000);

        (MetricsCollection<List<MetricSample>> metricSamples, _) = exporter.Export();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = metricSamples.Where(x => x.Key == metric).ToList();

            if (!exclusions.Contains(metric.Replace("System.Runtime.", string.Empty, StringComparison.Ordinal)))
            {
                Assert.NotNull(summary);
                Assert.True(summary.Count > 0, $"Expected metrics for {metric}");
            }
            else
            {
                Assert.True(summary == null || summary.Count == 0, $"Expected no metrics for {metric}");
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

        using var listener = new EventCounterListener(new MetricsObserverOptions
        {
            IncludedMetrics = inclusions
        });

        var exporter = new SteeltoeExporter(_exporterOptions);
        AggregationManager aggregationManager = GetTestMetrics(exporter);
        aggregationManager.Start();
        await Task.Delay(2000);

        (MetricsCollection<List<MetricSample>> metricSamples, _) = exporter.Export();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = metricSamples.Where(x => x.Key == metric).ToList();

            if (inclusions.Contains(metric.Substring("System.Runtime.".Length)))
            {
                Assert.NotNull(summary);
                Assert.True(summary.Count > 0, $"Expected metrics for {metric}");
            }
            else
            {
                Assert.True(summary == null || summary.Count == 0, $"Expected no metrics for {metric}");
            }
        }
    }
}

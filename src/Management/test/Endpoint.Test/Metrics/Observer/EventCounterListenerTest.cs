// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Steeltoe;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics.Observer;

public class EventCounterListenerTest : BaseTest
{
    private readonly PullMetricsExporterOptions _scraperOptions = new()
    {
        ScrapeResponseCacheDurationMilliseconds = 500
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
        OpenTelemetryMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var exporter = new SteeltoeExporter(_scraperOptions);
        using MeterProvider metrics = GetTestMetrics(null, exporter, null);
        await Task.Delay(2000);

        var collectionResponse = (SteeltoeCollectionResponse)await exporter.CollectionManager.EnterCollectAsync();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = collectionResponse.MetricSamples.Where(x => x.Key == metric).ToList();
            Assert.NotNull(summary);
            Assert.True(summary.Count > 0);
        }
    }

    [Fact]
    public async Task EventCounterListenerGetsMetricsWithExclusionsTest()
    {
        OpenTelemetryMetrics.InstrumentationName = Guid.NewGuid().ToString();

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

        var exporter = new SteeltoeExporter(_scraperOptions);
        using MeterProvider metrics = GetTestMetrics(null, exporter, null);
        await Task.Delay(2000);

        var collectionResponse = (SteeltoeCollectionResponse)await exporter.CollectionManager.EnterCollectAsync();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = collectionResponse.MetricSamples.Where(x => x.Key == metric).ToList();

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
        OpenTelemetryMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var inclusions = new List<string>
        {
            "cpu-usage"
        };

        using var listener = new EventCounterListener(new MetricsObserverOptions
        {
            IncludedMetrics = inclusions
        });

        var exporter = new SteeltoeExporter(_scraperOptions);
        using MeterProvider otelMetrics = GetTestMetrics(null, exporter, null);
        await Task.Delay(2000);

        var collectionResponse = (SteeltoeCollectionResponse)await exporter.CollectionManager.EnterCollectAsync();

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = collectionResponse.MetricSamples.Where(x => x.Key == metric).ToList();

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

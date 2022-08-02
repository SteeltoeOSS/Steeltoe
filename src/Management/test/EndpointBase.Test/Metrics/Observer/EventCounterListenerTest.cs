// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test;

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
    public void EventCounterListenerGetsMetricsTest()
    {
        using var listener = new EventCounterListener(new MetricsObserverOptions());
        OpenTelemetryMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var exporter = new SteeltoeExporter(_scraperOptions);
        using MeterProvider metrics = GetTestMetrics(null, exporter, null);
        Task.Delay(2000).Wait();

        var collectionResponse = (SteeltoeCollectionResponse)exporter.CollectionManager.EnterCollect().Result;

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = collectionResponse.MetricSamples.Where(x => x.Key == metric).ToList();
            Assert.NotNull(summary);
            Assert.True(summary.Count > 0);
        }
    }

    [Fact]
    public void EventCounterListenerGetsMetricsWithExclusionsTest()
    {
        var exclusions = new List<string>
        {
            "alloc-rate",
            "threadpool-completed-items-count",
            "gen-1-gc-count",
            "gen-1-size"
        };

        using var listener = new EventCounterListener(new MetricsObserverOptions
        {
            ExcludedMetrics = exclusions
        });

        var exporter = new SteeltoeExporter(_scraperOptions);
        using MeterProvider metrics = GetTestMetrics(null, exporter, null);
        Task.Delay(2000).Wait();

        var collectionResponse = (SteeltoeCollectionResponse)exporter.CollectionManager.EnterCollect().Result;

        foreach (string metric in _metrics)
        {
            List<KeyValuePair<string, List<MetricSample>>> summary = collectionResponse.MetricSamples.Where(x => x.Key == metric).ToList();

            if (!exclusions.Contains(metric.Replace("System.Runtime.", string.Empty)))
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

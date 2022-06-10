// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test;

[Obsolete]
public class HttpClientDesktopObserverTest : BaseTest
{
    [Fact]
    public void Constructor_RegistersExpectedViews()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var observer = new HttpClientDesktopObserver(options, null, viewRegistry);

        Assert.Contains(viewRegistry.Views, v => v.Key == "http.desktop.client.request.time");
        Assert.Contains(viewRegistry.Views, v => v.Key == "http.desktop.client.request.count");
    }

    [Fact]
    public void ShouldIgnore_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();

        var viewRegistry = new ViewRegistry();
        var observer = new HttpClientDesktopObserver(options, null, viewRegistry);

        Assert.True(observer.ShouldIgnoreRequest("/api/v2/spans"));
        Assert.True(observer.ShouldIgnoreRequest("/v2/apps/foobar/permissions"));
        Assert.True(observer.ShouldIgnoreRequest("/v2/apps/barfoo/permissions"));
        Assert.False(observer.ShouldIgnoreRequest("/api/test"));
        Assert.False(observer.ShouldIgnoreRequest("/v2/apps"));
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void ProcessEvent_IgnoresNulls()
#pragma warning restore S2699 // Tests should include assertions
    {
        var options = new MetricsObserverOptions();

        var viewRegistry = new ViewRegistry();
        var observer = new HttpClientDesktopObserver(options, null, viewRegistry);

        observer.ProcessEvent("foobar", null);
        observer.ProcessEvent(HttpClientDesktopObserver.STOP_EVENT, null);

        var act = new Activity("Test");
        act.Start();
        observer.ProcessEvent(HttpClientDesktopObserver.STOP_EVENT, null);
        observer.ProcessEvent(HttpClientDesktopObserver.STOPEX_EVENT, null);
        act.Stop();
    }

    [Fact]
    public void GetTagContext_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();

        var viewRegistry = new ViewRegistry();
        var observer = new HttpClientDesktopObserver(options, null, viewRegistry);

        var req = GetHttpRequestMessage();
        var labels = observer.GetLabels(req, HttpStatusCode.InternalServerError).ToList();

        Assert.Contains(KeyValuePair.Create("clientName", (object)"localhost:5555"), labels);
        Assert.Contains(KeyValuePair.Create("uri", (object)"/foo/bar"), labels);
        Assert.Contains(KeyValuePair.Create("status", (object)"500"), labels);
        Assert.Contains(KeyValuePair.Create("method", (object)"GET"), labels);
    }

    [Fact]
    public void HandleStopEvent_RecordsStats()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();

        OpenTelemetryMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var scraperOptions = new PullmetricsExporterOptions { ScrapeResponseCacheDurationMilliseconds = 10 };
        var observer = new HttpClientDesktopObserver(options, null, viewRegistry);
        var exporter = new SteeltoeExporter(scraperOptions);

        using var otelMetrics = GetTestMetrics(viewRegistry, exporter, null);

        var req = GetHttpRequestMessage();

        var act = new Activity("Test");
        act.Start();
        Thread.Sleep(1000);
        act.SetEndTime(DateTime.UtcNow);

        observer.HandleStopEvent(act, req, HttpStatusCode.InternalServerError);
        observer.HandleStopEvent(act, req, HttpStatusCode.OK);

        var collectionResponse = (SteeltoeCollectionResponse)exporter.CollectionManager.EnterCollect().Result;

        var timeSample = collectionResponse.MetricSamples.SingleOrDefault(x => x.Key == "http.desktop.client.request.time");
        Assert.NotNull(timeSample.Value);

        Func<MetricSample, MetricSample, MetricSample> sumAgg = (x, y) => new MetricSample(x.Statistic, x.Value + y.Value, x.Tags);

        var timeSummary = timeSample.Value.Aggregate(sumAgg);
        var countSample = collectionResponse.MetricSamples.SingleOrDefault(x => x.Key == "http.desktop.client.request.count");
        var countSummary = countSample.Value.Aggregate(sumAgg);

        var average = timeSummary.Value / countSummary.Value;
        Assert.InRange(average, 950.0, 1500.0);

        Assert.Equal(2, countSummary.Value);

        // TODO: Readd when aggregations are available
        // Assert.InRange(processor.GetMetricByName<double>((string)"http.desktop.client.request.time").Min, 950.0, 1500.0);
        // Assert.InRange(processor.GetMetricByName<double>((string)"http.desktop.client.request.time").Max, 950.0, 1500.0);
        act.Stop();
    }

    private HttpWebRequest GetHttpRequestMessage()
    {
        var m = WebRequest.CreateHttp("http://localhost:5555/foo/bar");
        m.Method = HttpMethod.Get.Method;
        return m;
    }
}

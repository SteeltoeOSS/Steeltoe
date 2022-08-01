// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test;

[Obsolete("To be removed in the next major version.")]
public class HttpClientCoreObserverTest : BaseTest
{
    private readonly PullMetricsExporterOptions _scraperOptions = new () { ScrapeResponseCacheDurationMilliseconds = 100 };

    [Fact]
    public void Constructor_RegistersExpectedViews()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        _ = new HttpClientCoreObserver(options, null, viewRegistry);

        Assert.Contains(viewRegistry.Views, v => v.Key == "http.client.request.time");
        Assert.Contains(viewRegistry.Views, v => v.Key == "http.client.request.count");
    }

    [Fact]
    public void ShouldIgnore_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var obs = new HttpClientCoreObserver(options, null, viewRegistry);

        Assert.True(obs.ShouldIgnoreRequest("/api/v2/spans"));
        Assert.True(obs.ShouldIgnoreRequest("/v2/apps/foobar/permissions"));
        Assert.True(obs.ShouldIgnoreRequest("/v2/apps/barfoo/permissions"));
        Assert.False(obs.ShouldIgnoreRequest("/api/test"));
        Assert.False(obs.ShouldIgnoreRequest("/v2/apps"));
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void ProcessEvent_IgnoresNulls()
#pragma warning restore S2699 // Tests should include assertions
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var obs = new HttpClientCoreObserver(options, null, viewRegistry);

        obs.ProcessEvent("foobar", null);
        obs.ProcessEvent(HttpClientCoreObserver.StopEvent, null);

        var act = new Activity("Test");
        act.Start();
        obs.ProcessEvent(HttpClientCoreObserver.StopEvent, null);
        obs.ProcessEvent(HttpClientCoreObserver.ExceptionEvent, null);
        act.Stop();
    }

    [Fact]
    public void GetStatusCode_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var obs = new HttpClientCoreObserver(options, null, viewRegistry);

        var message = GetHttpResponseMessage(HttpStatusCode.OK);
        var status = obs.GetStatusCode(message, default);
        Assert.Equal("200", status);

        status = obs.GetStatusCode(null, TaskStatus.Canceled);
        Assert.Equal("CLIENT_CANCELED", status);

        status = obs.GetStatusCode(null, TaskStatus.Faulted);
        Assert.Equal("CLIENT_FAULT", status);

        status = obs.GetStatusCode(null, TaskStatus.RanToCompletion);
        Assert.Equal("CLIENT_ERROR", status);
    }

    [Fact]
    public void GetTagContext_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var obs = new HttpClientCoreObserver(options, null, viewRegistry);

        var req = GetHttpRequestMessage();
        var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);
        var tagContext = obs.GetLabels(req, resp, TaskStatus.RanToCompletion);
        var tagValues = tagContext.ToList();

        Assert.Contains(KeyValuePair.Create("clientName", (object)"localhost:5555"), tagValues);
        Assert.Contains(KeyValuePair.Create("uri", (object)"/foo/bar"), tagValues);
        Assert.Contains(KeyValuePair.Create("status", (object)"500"), tagValues);
        Assert.Contains(KeyValuePair.Create("method", (object)"GET"), tagValues);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void HandleStopEvent_RecordsStats()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();

        OpenTelemetryMetrics.InstrumentationName = Guid.NewGuid().ToString();

        var exporter = new SteeltoeExporter(_scraperOptions);
        using var metrics = GetTestMetrics(viewRegistry, exporter, null);
        var observer = new HttpClientCoreObserver(options, null, viewRegistry);

        var req = GetHttpRequestMessage();
        var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);

        var act = new Activity("Test");
        act.Start();

        Task.Delay(1000).Wait();
        act.SetEndTime(DateTime.UtcNow);

        observer.HandleStopEvent(act, req, resp, TaskStatus.RanToCompletion);
        observer.HandleStopEvent(act, req, resp, TaskStatus.RanToCompletion);

        var collectionResponse = (SteeltoeCollectionResponse)exporter.CollectionManager.EnterCollect().Result;

        var timeSample = collectionResponse.MetricSamples.SingleOrDefault(x => x.Key == "http.client.request.time");
        var timeSummary = timeSample.Value.FirstOrDefault();
        var countSample = collectionResponse.MetricSamples.SingleOrDefault(x => x.Key == "http.client.request.count");
        var countSummary = countSample.Value.FirstOrDefault();

        Assert.NotNull(timeSample.Value);

        var average = timeSummary.Value / countSummary.Value;
        Assert.InRange(average, 975.0, 1200.0);

        // Assert.InRange(max, 975.0, 1200.0);
        // TODO: Read when aggregations are available
        Assert.Equal(2, countSummary.Value);

        act.Stop();
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void HandleExceptionEvent_RecordsStats()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();

        OpenTelemetryMetrics.InstrumentationName = Guid.NewGuid().ToString();
        var observer = new HttpClientCoreObserver(options, null, viewRegistry);

        var exporter = new SteeltoeExporter(_scraperOptions);
        using var metrics = GetTestMetrics(viewRegistry, exporter, null);

        var req = GetHttpRequestMessage();

        var act = new Activity("Test");
        act.Start();
        Task.Delay(1000).Wait();
        act.SetEndTime(DateTime.UtcNow);

        observer.HandleExceptionEvent(act, req);
        observer.HandleExceptionEvent(act, req);

        var collectionResponse = (SteeltoeCollectionResponse)exporter.CollectionManager.EnterCollect().Result;

        var timeSample = collectionResponse.MetricSamples.SingleOrDefault(x => x.Key == "http.client.request.time");
        var timeSummary = timeSample.Value.FirstOrDefault();
        var countSample = collectionResponse.MetricSamples.SingleOrDefault(x => x.Key == "http.client.request.count");
        var countSummary = countSample.Value.FirstOrDefault();

        Assert.NotNull(timeSample.Value);

        var average = timeSummary.Value / countSummary.Value;
        Assert.InRange(average, 975.0, 1200.0);

        // Assert.InRange(max, 975.0, 1200.0);
        // TODO: Read when aggregations are available
        Assert.Equal(2, countSummary.Value);

        act.Stop();
    }

    private HttpResponseMessage GetHttpResponseMessage(HttpStatusCode code)
    {
        var m = new HttpResponseMessage(code);
        return m;
    }

    private HttpRequestMessage GetHttpRequestMessage()
    {
        var m = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5555/foo/bar");
        return m;
    }
}

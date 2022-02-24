//Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

//using Steeltoe.Management.Endpoint.Test;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Xunit;

//namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
//{
//    [Obsolete]
//    public class HttpClientCoreObserverTest : BaseTest
//    {
//        // TODO: Pending View API
//        /*
//        [Fact]
//        public void Constructor_RegistersExpectedViews()
//        {
//            var options = new MetricsEndpointOptions();
//            var stats = new OpenCensusStats();
//            var tags = new OpenCensusTags();
//            var observer = new HttpClientCoreObserver(options, stats, tags, null);

//            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.client.request.time")));
//            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.client.request.count")));
//        }
//        */

//        [Fact]
//        public void ShouldIgnore_ReturnsExpected()
//        {
//            var options = new MetricsObserverOptions();
//            var stats = new TestOpenTelemetryMetrics();
//            var obs = new HttpClientCoreObserver(options, stats, null);

//            Assert.True(obs.ShouldIgnoreRequest("/api/v2/spans"));
//            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/foobar/permissions"));
//            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/barfoo/permissions"));
//            Assert.False(obs.ShouldIgnoreRequest("/api/test"));
//            Assert.False(obs.ShouldIgnoreRequest("/v2/apps"));
//        }

//        [Fact]
//        public void ProcessEvent_IgnoresNulls()
//        {
//            var options = new MetricsObserverOptions();
//            var stats = new TestOpenTelemetryMetrics();
//            var observer = new HttpClientCoreObserver(options, stats, null);

//            observer.ProcessEvent("foobar", null);
//            observer.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, null);

//            var act = new Activity("Test");
//            act.Start();
//            observer.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, null);
//            observer.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, null);
//            act.Stop();
//        }

//        [Fact]
//        public void GetStatusCode_ReturnsExpected()
//        {
//            var options = new MetricsObserverOptions();
//            var stats = new TestOpenTelemetryMetrics();
//            var observer = new HttpClientCoreObserver(options, stats, null);

//            var message = GetHttpResponseMessage(HttpStatusCode.OK);
//            var status = observer.GetStatusCode(message, default);
//            Assert.Equal("200", status);

//            status = observer.GetStatusCode(null, TaskStatus.Canceled);
//            Assert.Equal("CLIENT_CANCELED", status);

//            status = observer.GetStatusCode(null, TaskStatus.Faulted);
//            Assert.Equal("CLIENT_FAULT", status);

//            status = observer.GetStatusCode(null, TaskStatus.RanToCompletion);
//            Assert.Equal("CLIENT_ERROR", status);
//        }

//        [Fact]
//        public void GetTagContext_ReturnsExpected()
//        {
//            var options = new MetricsObserverOptions();
//            var stats = new TestOpenTelemetryMetrics();
//            var observer = new HttpClientCoreObserver(options, stats, null);

//            var req = GetHttpRequestMessage();
//            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);
//            var tagContext = observer.GetLabels(req, resp, TaskStatus.RanToCompletion);
//            var tagValues = tagContext.ToList();
//            tagValues.Contains(KeyValuePair.Create("clientName", "localhost:5555"));
//            tagValues.Contains(KeyValuePair.Create("uri", "/foo/bar"));
//            tagValues.Contains(KeyValuePair.Create("status", "500"));
//            tagValues.Contains(KeyValuePair.Create("method", "GET"));
//        }

//        [Fact]
//        [Trait("Category", "FlakyOnHostedAgents")]
//        public void HandleStopEvent_RecordsStats()
//        {
//            var options = new MetricsObserverOptions();
//            var stats = new TestOpenTelemetryMetrics();
//            var observer = new HttpClientCoreObserver(options, stats, null);
//            var factory = stats.Factory;
//            var processor = stats.Processor;

//            var req = GetHttpRequestMessage();
//            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);

//            var act = new Activity("Test");
//            act.Start();

//            Task.Delay(1000).Wait();
//            act.SetEndTime(DateTime.UtcNow);

//            observer.HandleStopEvent(act, req, resp, TaskStatus.RanToCompletion);
//            observer.HandleStopEvent(act, req, resp, TaskStatus.RanToCompletion);

//            factory.CollectAllMetrics();

//            var timeSummary = processor.GetMetricByName<double>("http.client.request.time");
//            Assert.NotNull(timeSummary);
//            var average = timeSummary.Sum / timeSummary.Count;
//            Assert.InRange(average, 975.0, 1200.0);
//            Assert.InRange(timeSummary.Max, 975.0, 1200.0);

//            var countSummary = processor.GetMetricByName<long>("http.client.request.count");
//            Assert.Equal(2, countSummary.Count);

//            act.Stop();
//        }

//        [Fact]
//        [Trait("Category", "FlakyOnHostedAgents")]
//        public void HandleExceptionEvent_RecordsStats()
//        {
//            var options = new MetricsObserverOptions();
//            var stats = new TestOpenTelemetryMetrics();
//            var observer = new HttpClientCoreObserver(options, stats, null);
//            var factory = stats.Factory;
//            var processor = stats.Processor;

//            var req = GetHttpRequestMessage();
//            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);

//            var act = new Activity("Test");
//            act.Start();
//            Task.Delay(1000).Wait();
//            act.SetEndTime(DateTime.UtcNow);

//            observer.HandleExceptionEvent(act, req);
//            observer.HandleExceptionEvent(act, req);

//            factory.CollectAllMetrics();

//            var timeSummary = processor.GetMetricByName<double>("http.client.request.time");
//            Assert.NotNull(timeSummary);
//            var average = timeSummary.Sum / timeSummary.Count;
//            Assert.InRange(average, 990.0, 1200.0);
//            Assert.InRange(timeSummary.Max, 990.0, 1200.0);

//            var countSummary = processor.GetMetricByName<long>("http.client.request.count");
//            Assert.Equal(2, countSummary.Count);

//            act.Stop();
//        }

//        private HttpResponseMessage GetHttpResponseMessage(HttpStatusCode code)
//        {
//            var m = new HttpResponseMessage(code);
//            return m;
//        }

//        private HttpRequestMessage GetHttpRequestMessage()
//        {
//            var m = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5555/foo/bar");
//            return m;
//        }
//    }
//}

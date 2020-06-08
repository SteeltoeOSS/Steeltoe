// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointBase.Test.Metrics;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    public class HttpClientDesktopObserverTest : BaseTest
    {
        // Bring back with Views API
        /* [Fact]
         public void Constructor_RegistersExpectedViews()
         {
             var options = new MetricsEndpointOptions();
             var stats = new OpenCensusStats();
             var tags = new OpenCensusTags();
             var observer = new HttpClientDesktopObserver(options, stats, tags, null);

             Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.desktop.client.request.time")));
             Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.desktop.client.request.count")));
         }*/

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new HttpClientDesktopObserver(options, stats, null);

            Assert.True(observer.ShouldIgnoreRequest("/api/v2/spans"));
            Assert.True(observer.ShouldIgnoreRequest("/v2/apps/foobar/permissions"));
            Assert.True(observer.ShouldIgnoreRequest("/v2/apps/barfoo/permissions"));
            Assert.False(observer.ShouldIgnoreRequest("/api/test"));
            Assert.False(observer.ShouldIgnoreRequest("/v2/apps"));
        }

        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new HttpClientDesktopObserver(options, stats, null);

            observer.ProcessEvent("foobar", null);
            observer.ProcessEvent(HttpClientDesktopObserver.STOP_EVENT, null);

            Activity act = new Activity("Test");
            act.Start();
            observer.ProcessEvent(HttpClientDesktopObserver.STOP_EVENT, null);
            observer.ProcessEvent(HttpClientDesktopObserver.STOPEX_EVENT, null);
            act.Stop();
        }

        [Fact]
        public void GetTagContext_ReturnsExpected()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new HttpClientDesktopObserver(options, stats, null);

            var req = GetHttpRequestMessage();
            var labels = observer.GetLabels(req, HttpStatusCode.InternalServerError);
            labels.Contains(KeyValuePair.Create("clientName", "localhost:5555"));
            labels.Contains(KeyValuePair.Create("uri", "/foo/bar"));
            labels.Contains(KeyValuePair.Create("status", "500"));
            labels.Contains(KeyValuePair.Create("method", "GET"));
        }

        [Fact]
        public void HandleStopEvent_RecordsStats()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new HttpClientDesktopObserver(options, stats, null);
            var factory = stats.Factory;
            var processor = stats.Processor;

            var req = GetHttpRequestMessage();

            Activity act = new Activity("Test");
            act.Start();
            Thread.Sleep(1000);
            act.SetEndTime(DateTime.UtcNow);

            observer.HandleStopEvent(act, req, HttpStatusCode.InternalServerError);
            observer.HandleStopEvent(act, req, HttpStatusCode.OK);

            factory.CollectAllMetrics();

            var requestTime = processor.GetMetricByName<double>("http.desktop.client.request.time");
            Assert.NotNull(requestTime);
            Assert.InRange(requestTime.Min, 950.0, 1500.0);
            Assert.InRange(requestTime.Max, 950.0, 1500.0);

            var requestCount = processor.GetMetricByName<long>("http.desktop.client.request.count");
            Assert.NotNull(requestCount);
            Assert.Equal(2, requestCount.Sum);

            act.Stop();
        }

        private HttpWebRequest GetHttpRequestMessage()
        {
            var m = WebRequest.CreateHttp("http://localhost:5555/foo/bar");
            m.Method = HttpMethod.Get.Method;
            return m;
        }
    }
}

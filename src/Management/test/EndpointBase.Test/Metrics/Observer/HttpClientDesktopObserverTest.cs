// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint.Test;
using System;
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
        [Fact]
        public void Constructor_RegistersExpectedViews()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientDesktopObserver(options, stats, tags, null);

            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.desktop.client.request.time")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.desktop.client.request.count")));
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var obs = new HttpClientDesktopObserver(options, stats, tags, null);

            Assert.True(obs.ShouldIgnoreRequest("/api/v2/spans"));
            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/foobar/permissions"));
            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/barfoo/permissions"));
            Assert.False(obs.ShouldIgnoreRequest("/api/test"));
            Assert.False(obs.ShouldIgnoreRequest("/v2/apps"));
        }

        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientDesktopObserver(options, stats, tags, null);

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
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientDesktopObserver(options, stats, tags, null);

            var req = GetHttpRequestMessage();
            var tagContext = observer.GetTagContext(req, HttpStatusCode.InternalServerError);
            var tagValues = tagContext.ToList();
            tagValues.Contains(Tag.Create(TagKey.Create("clientName"), TagValue.Create("localhost:5555")));
            tagValues.Contains(Tag.Create(TagKey.Create("uri"), TagValue.Create("/foo/bar")));
            tagValues.Contains(Tag.Create(TagKey.Create("status"), TagValue.Create("500")));
            tagValues.Contains(Tag.Create(TagKey.Create("method"), TagValue.Create("GET")));
        }

        [Fact]
        public void HandleStopEvent_RecordsStats()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientDesktopObserver(options, stats, tags, null);

            var req = GetHttpRequestMessage();

            var act = new Activity("Test");
            act.Start();
            Thread.Sleep(1000);
            act.SetEndTime(DateTime.UtcNow);

            observer.HandleStopEvent(act, req, HttpStatusCode.InternalServerError);
            observer.HandleStopEvent(act, req, HttpStatusCode.OK);

            var reqData = stats.ViewManager.GetView(ViewName.Create("http.desktop.client.request.time"));
            var aggData1 = MetricsHelpers.SumWithTags(reqData) as IDistributionData;
            Assert.InRange(aggData1.Mean, 950.0, 1500.0);
            Assert.InRange(aggData1.Max, 950.0, 1500.0);

            reqData = stats.ViewManager.GetView(ViewName.Create("http.desktop.client.request.count"));
            var aggData2 = MetricsHelpers.SumWithTags(reqData) as ISumDataLong;
            Assert.Equal(2, aggData2.Sum);

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

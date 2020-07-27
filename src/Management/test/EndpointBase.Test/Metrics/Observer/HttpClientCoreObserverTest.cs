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
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    public class HttpClientCoreObserverTest : BaseTest
    {
        [Fact]
        public void Constructor_RegistersExpectedViews()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.client.request.time")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.client.request.count")));
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var obs = new HttpClientCoreObserver(options, stats, tags, null);

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
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            observer.ProcessEvent("foobar", null);
            observer.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, null);

            var act = new Activity("Test");
            act.Start();
            observer.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, null);
            observer.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, null);
            act.Stop();
        }

        [Fact]
        public void GetStatusCode_ReturnsExpected()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            var message = GetHttpResponseMessage(HttpStatusCode.OK);
            var status = observer.GetStatusCode(message, default);
            Assert.Equal("200", status);

            status = observer.GetStatusCode(null, TaskStatus.Canceled);
            Assert.Equal("CLIENT_CANCELED", status);

            status = observer.GetStatusCode(null, TaskStatus.Faulted);
            Assert.Equal("CLIENT_FAULT", status);

            status = observer.GetStatusCode(null, TaskStatus.RanToCompletion);
            Assert.Equal("CLIENT_ERROR", status);
        }

        [Fact]
        public void GetTagContext_ReturnsExpected()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            var req = GetHttpRequestMessage();
            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);
            var tagContext = observer.GetTagContext(req, resp, TaskStatus.RanToCompletion);
            var tagValues = tagContext.ToList();
            tagValues.Contains(Tag.Create(TagKey.Create("clientName"), TagValue.Create("localhost:5555")));
            tagValues.Contains(Tag.Create(TagKey.Create("uri"), TagValue.Create("/foo/bar")));
            tagValues.Contains(Tag.Create(TagKey.Create("status"), TagValue.Create("500")));
            tagValues.Contains(Tag.Create(TagKey.Create("method"), TagValue.Create("GET")));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void HandleStopEvent_RecordsStats()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            var req = GetHttpRequestMessage();
            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);

            var act = new Activity("Test");
            act.Start();
            Thread.Sleep(1000);
            act.SetEndTime(DateTime.UtcNow);

            observer.HandleStopEvent(act, req, resp, TaskStatus.RanToCompletion);
            observer.HandleStopEvent(act, req, resp, TaskStatus.RanToCompletion);

            var reqData = stats.ViewManager.GetView(ViewName.Create("http.client.request.time"));
            var aggData1 = MetricsHelpers.SumWithTags(reqData) as IDistributionData;
            Assert.True(aggData1.Mean >= 1000.00);
            Assert.True(aggData1.Max >= 1000.00);

            reqData = stats.ViewManager.GetView(ViewName.Create("http.client.request.count"));
            var aggData2 = MetricsHelpers.SumWithTags(reqData) as ISumDataLong;
            Assert.Equal(2, aggData2.Sum);

            act.Stop();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void HandleExceptionEvent_RecordsStats()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            var req = GetHttpRequestMessage();
            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);

            var act = new Activity("Test");
            act.Start();
            Thread.Sleep(1000);
            act.SetEndTime(DateTime.UtcNow);

            observer.HandleExceptionEvent(act, req);
            observer.HandleExceptionEvent(act, req);

            var reqData = stats.ViewManager.GetView(ViewName.Create("http.client.request.time"));
            var aggData1 = MetricsHelpers.SumWithTags(reqData) as IDistributionData;
            Assert.InRange(aggData1.Mean, 995.0, 1200.0);
            Assert.InRange(aggData1.Max, 995.0, 1200.0);

            reqData = stats.ViewManager.GetView(ViewName.Create("http.client.request.count"));
            var aggData2 = MetricsHelpers.SumWithTags(reqData) as ISumDataLong;
            Assert.Equal(2, aggData2.Sum);

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
}

// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

            Activity act = new Activity("Test");
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
            string status = observer.GetStatusCode(message, default(TaskStatus));
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
        public void HandleStopEvent_RecordsStats()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            var req = GetHttpRequestMessage();
            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);

            Activity act = new Activity("Test");
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
        public void HandleExceptionEvent_RecordsStats()
        {
            var options = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tags = new OpenCensusTags();
            var observer = new HttpClientCoreObserver(options, stats, tags, null);

            var req = GetHttpRequestMessage();
            var resp = GetHttpResponseMessage(HttpStatusCode.InternalServerError);

            Activity act = new Activity("Test");
            act.Start();
            Thread.Sleep(1000);
            act.SetEndTime(DateTime.UtcNow);

            observer.HandleExceptionEvent(act, req);
            observer.HandleExceptionEvent(act, req);

            var reqData = stats.ViewManager.GetView(ViewName.Create("http.client.request.time"));
            var aggData1 = MetricsHelpers.SumWithTags(reqData) as IDistributionData;
            Assert.InRange(aggData1.Mean, 995.0, 1005.0);
            Assert.InRange(aggData1.Max, 995.0, 1005.0);

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

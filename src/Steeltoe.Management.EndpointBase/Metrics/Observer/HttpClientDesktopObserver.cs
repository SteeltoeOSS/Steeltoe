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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class HttpClientDesktopObserver : MetricsObserver
    {
        internal const string DIAGNOSTIC_NAME = "System.Net.Http.Desktop";
        internal const string OBSERVER_NAME = "HttpClientDesktopObserver";

        internal const string STOP_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Stop";
        internal const string STOPEX_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";

        private readonly ITagKey statusTagKey = TagKey.Create("status");
        private readonly ITagKey uriTagKey = TagKey.Create("uri");
        private readonly ITagKey methodTagKey = TagKey.Create("method");
        private readonly ITagKey clientTagKey = TagKey.Create("clientName");

        private readonly IMeasureDouble clientTimeMeasure;
        private readonly IMeasureLong clientCountMeasure;

        public HttpClientDesktopObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<HttpClientDesktopObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            PathMatcher = new Regex(options.EgressIgnorePattern);

            clientTimeMeasure = MeasureDouble.Create("client.desk.totalTime", "Total request time", MeasureUnit.MilliSeconds);
            clientCountMeasure = MeasureLong.Create("client.core.totalRequests", "Total request count", "count");

            var view = View.Create(
                    ViewName.Create("http.desktop.client.request.time"),
                    "Total request time",
                    clientTimeMeasure,
                     Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
                    new List<ITagKey>() { statusTagKey, uriTagKey, methodTagKey, clientTagKey });

            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("http.desktop.client.request.count"),
                        "Total request counts",
                        clientCountMeasure,
                        Sum.Create(),
                         new List<ITagKey>() { statusTagKey, uriTagKey, methodTagKey, clientTagKey });
            ViewManager.RegisterView(view);
        }

        public override void ProcessEvent(string evnt, object arg)
        {
            if (arg == null)
            {
                return;
            }

            Activity current = Activity.Current;
            if (current == null)
            {
                return;
            }

            var request = DiagnosticHelpers.GetProperty<HttpWebRequest>(arg, "Request");
            if (request == null)
            {
                return;
            }

            if (evnt == STOP_EVENT)
            {
                Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                var response = DiagnosticHelpers.GetProperty<HttpWebResponse>(arg, "Response");
                if (response != null)
                {
                    HandleStopEvent(current, request, response.StatusCode);
                }

                Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == STOPEX_EVENT)
            {
                Logger?.LogTrace("HandleStopEventEx start {thread}", Thread.CurrentThread.ManagedThreadId);

                var statusCode = DiagnosticHelpers.GetProperty<HttpStatusCode>(arg, "StatusCode");

                HandleStopEvent(current, request, statusCode);

                Logger?.LogTrace("HandleStopEventEx finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleStopEvent(Activity current, HttpWebRequest request, HttpStatusCode statusCode)
        {
            if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
            {
                Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", request.RequestUri.AbsolutePath);
                return;
            }

            if (current.Duration.TotalMilliseconds > 0)
            {
                ITagContext tagContext = GetTagContext(request, statusCode);
                StatsRecorder
                    .NewMeasureMap()
                    .Put(clientTimeMeasure, current.Duration.TotalMilliseconds)
                    .Put(clientCountMeasure, 1)
                    .Record(tagContext);
            }
        }

        protected internal ITagContext GetTagContext(HttpWebRequest request, HttpStatusCode status)
        {
            var uri = request.RequestUri.ToString();
            var statusCode = status.ToString();

            return Tagger
                .EmptyBuilder
                .Put(uriTagKey, TagValue.Create(uri))
                .Put(statusTagKey, TagValue.Create(statusCode))
                .Put(clientTagKey, TagValue.Create(request.RequestUri.Host))
                .Put(methodTagKey, TagValue.Create(request.Method.ToString()))
                .Build();
        }
    }
}

// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Stats;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        private readonly string statusTagKey = "status";
        private readonly string uriTagKey = "uri";
        private readonly string methodTagKey = "method";
        private readonly string clientTagKey = "clientName";

        private readonly MeasureMetric<double> clientTimeMeasure;
        private readonly MeasureMetric<long> clientCountMeasure;

        public HttpClientDesktopObserver(IMetricsOptions options, IStats stats, ILogger<HttpClientDesktopObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, stats, logger)
        {
            PathMatcher = new Regex(options.EgressIgnorePattern);

            clientTimeMeasure = Meter.CreateDoubleMeasure("http.desktop.client.request.time");
            clientCountMeasure = Meter.CreateInt64Measure("http.desktop.client.request.count");

            // Bring back views when available
            /*var view = View.Create(
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
            ViewManager.RegisterView(view);*/
        }

        public override void ProcessEvent(string evnt, object arg)
        {
            if (arg == null)
            {
                return;
            }

            var current = Activity.Current;
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
                Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", SecurityUtilities.SanitizeInput(request.RequestUri.AbsolutePath));
                return;
            }

            if (current.Duration.TotalMilliseconds > 0)
            {
                var labels = GetLabels(request, statusCode);
                clientTimeMeasure.Record(default(SpanContext), current.Duration.TotalMilliseconds, labels);
                clientCountMeasure.Record(default(SpanContext), 1, labels);
            }
        }

        protected internal List<KeyValuePair<string, string>> GetLabels(HttpWebRequest request, HttpStatusCode statusCode)
        {
            return new Dictionary<string, string>()
                    {
                        { uriTagKey, request.RequestUri.ToString() },
                        { statusTagKey, statusCode.ToString() },
                        { clientTagKey, request.RequestUri.Host },
                        { methodTagKey, request.Method }
                    }.ToList();
        }
    }
}

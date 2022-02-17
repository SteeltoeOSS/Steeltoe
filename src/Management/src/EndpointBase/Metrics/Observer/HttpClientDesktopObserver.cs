// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class HttpClientDesktopObserver : MetricsObserver
    {
        internal const string DIAGNOSTIC_NAME = "System.Net.Http.Desktop";
        internal const string OBSERVER_NAME = "HttpClientDesktopObserver";

        internal const string STOP_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Stop";
        internal const string STOPEX_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";

        private readonly string _statusTagKey = "status";
        private readonly string _uriTagKey = "uri";
        private readonly string _methodTagKey = "method";
        private readonly string _clientTagKey = "clientName";
        private Histogram<double> _clientTimeMeasure;
        private Histogram<double> _clientCountMeasure;

        public HttpClientDesktopObserver(IMetricsObserverOptions options, ILogger<HttpClientDesktopObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, logger)
        {
            PathMatcher = new Regex(options.EgressIgnorePattern);

            _clientTimeMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.time");
            _clientCountMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.count");

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
                _clientTimeMeasure.Record(current.Duration.TotalMilliseconds, labels.AsReadonlySpan());
                _clientCountMeasure.Record( 1, labels.AsReadonlySpan());
            }
        }

        protected internal IEnumerable<KeyValuePair<string, object>> GetLabels(HttpWebRequest request, HttpStatusCode statusCode)
        {
            return new Dictionary<string, object>()
                    {
                        { _uriTagKey, request.RequestUri.ToString() },
                        { _statusTagKey, statusCode.ToString() },
                        { _clientTagKey, request.RequestUri.Host },
                        { _methodTagKey, request.Method }
                    }.ToList();
        }
    }
}

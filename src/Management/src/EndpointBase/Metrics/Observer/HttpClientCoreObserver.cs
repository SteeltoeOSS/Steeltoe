// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class HttpClientCoreObserver : MetricsObserver
    {
        internal const string DIAGNOSTIC_NAME = "HttpHandlerDiagnosticListener";
        internal const string OBSERVER_NAME = "HttpClientCoreObserver";

        internal const string STOP_EVENT = "System.Net.Http.HttpRequestOut.Stop";
        internal const string EXCEPTION_EVENT = "System.Net.Http.Exception";

        private readonly string _statusTagKey = "status";
        private readonly string _uriTagKey = "uri";
        private readonly string _methodTagKey = "method";
        private readonly string _clientTagKey = "clientName";

        private readonly MeasureMetric<double> _clientTimeMeasure;
        private readonly MeasureMetric<long> _clientCountMeasure;

        public HttpClientCoreObserver(IMetricsObserverOptions options, IStats stats, ILogger<HttpClientCoreObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, stats, logger)
        {
            PathMatcher = new Regex(options.EgressIgnorePattern);
            _clientTimeMeasure = Meter.CreateDoubleMeasure("http.client.request.time");
            _clientCountMeasure = Meter.CreateInt64Measure("http.client.request.count");

            /* TODO: figureout bound instruments & view API
            var view = View.Create(
                    ViewName.Create("http.client.request.time"),
                    "Total request time",
                    clientTimeMeasure,
                    Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
                    new List<ITagKey>() { statusTagKey, uriTagKey, methodTagKey, clientTagKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                ViewName.Create("http.client.request.count"),
                "Total request counts",
                clientCountMeasure,
                Sum.Create(),
                new List<ITagKey>() { statusTagKey, uriTagKey, methodTagKey, clientTagKey });

            ViewManager.RegisterView(view);
            */
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

            var request = DiagnosticHelpers.GetProperty<HttpRequestMessage>(arg, "Request");
            if (request == null)
            {
                return;
            }

            if (evnt == STOP_EVENT)
            {
                Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                var response = DiagnosticHelpers.GetProperty<HttpResponseMessage>(arg, "Response");
                var requestStatus = DiagnosticHelpers.GetProperty<TaskStatus>(arg, "RequestTaskStatus");
                HandleStopEvent(current, request, response, requestStatus);

                Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == EXCEPTION_EVENT)
            {
                Logger?.LogTrace("HandleExceptionEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                HandleExceptionEvent(current, request);

                Logger?.LogTrace("HandleExceptionEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleExceptionEvent(Activity current, HttpRequestMessage request)
        {
            HandleStopEvent(current, request, null, TaskStatus.Faulted);
        }

        protected internal void HandleStopEvent(Activity current, HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
        {
            if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
            {
                Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", SecurityUtilities.SanitizeInput(request.RequestUri.AbsolutePath));
                return;
            }

            if (current.Duration.TotalMilliseconds > 0)
            {
                var labels = GetLabels(request, response, taskStatus);
                _clientTimeMeasure.Record(default(SpanContext), current.Duration.TotalMilliseconds, labels);
                _clientCountMeasure.Record(default(SpanContext), 1, labels);
            }
        }

        protected internal IEnumerable<KeyValuePair<string, string>> GetLabels(HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
        {
            var uri = request.RequestUri.ToString();
            var statusCode = GetStatusCode(response, taskStatus);
            var labels = new List<KeyValuePair<string, string>>();
            labels.Add(KeyValuePair.Create(_uriTagKey, uri));
            labels.Add(KeyValuePair.Create(_statusTagKey, statusCode));
            labels.Add(KeyValuePair.Create(_clientTagKey, request.RequestUri.Host));
            labels.Add(KeyValuePair.Create(_methodTagKey, request.Method.ToString()));
            return labels;
        }

        protected internal string GetStatusCode(HttpResponseMessage response, TaskStatus taskStatus)
        {
            if (response != null)
            {
                var val = (int)response.StatusCode;
                return val.ToString();
            }

            if (taskStatus == TaskStatus.Faulted)
            {
                return "CLIENT_FAULT";
            }

            if (taskStatus == TaskStatus.Canceled)
            {
                return "CLIENT_CANCELED";
            }

            return "CLIENT_ERROR";
        }
    }
}

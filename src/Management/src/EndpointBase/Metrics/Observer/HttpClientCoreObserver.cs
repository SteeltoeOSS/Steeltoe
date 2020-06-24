// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class HttpClientCoreObserver : MetricsObserver
    {
        internal const string DIAGNOSTIC_NAME = "HttpHandlerDiagnosticListener";
        internal const string OBSERVER_NAME = "HttpClientCoreObserver";

        internal const string STOP_EVENT = "System.Net.Http.HttpRequestOut.Stop";
        internal const string EXCEPTION_EVENT = "System.Net.Http.Exception";

        private readonly ITagKey _statusTagKey = TagKey.Create("status");
        private readonly ITagKey _uriTagKey = TagKey.Create("uri");
        private readonly ITagKey _methodTagKey = TagKey.Create("method");
        private readonly ITagKey _clientTagKey = TagKey.Create("clientName");

        private readonly IMeasureDouble _clientTimeMeasure;
        private readonly IMeasureLong _clientCountMeasure;

        public HttpClientCoreObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<HttpClientCoreObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            PathMatcher = new Regex(options.EgressIgnorePattern);

            _clientTimeMeasure = MeasureDouble.Create("client.core.totalTime", "Total request time", MeasureUnit.MilliSeconds);
            _clientCountMeasure = MeasureLong.Create("client.core.totalRequests", "Total request count", "count");

            var view = View.Create(
                    ViewName.Create("http.client.request.time"),
                    "Total request time",
                    _clientTimeMeasure,
                    Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
                    new List<ITagKey>() { _statusTagKey, _uriTagKey, _methodTagKey, _clientTagKey });
            ViewManager.RegisterView(view);

            view = View.Create(
                ViewName.Create("http.client.request.count"),
                "Total request counts",
                _clientCountMeasure,
                Sum.Create(),
                new List<ITagKey>() { _statusTagKey, _uriTagKey, _methodTagKey, _clientTagKey });

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
                ITagContext tagContext = GetTagContext(request, response, taskStatus);
                StatsRecorder
                    .NewMeasureMap()
                    .Put(_clientTimeMeasure, current.Duration.TotalMilliseconds)
                    .Put(_clientCountMeasure, 1)
                    .Record(tagContext);
            }
        }

        protected internal ITagContext GetTagContext(HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
        {
            var uri = request.RequestUri.ToString();
            var statusCode = GetStatusCode(response, taskStatus);

            return Tagger
                .EmptyBuilder
                .Put(_uriTagKey, TagValue.Create(uri))
                .Put(_statusTagKey, TagValue.Create(statusCode))
                .Put(_clientTagKey, TagValue.Create(request.RequestUri.Host))
                .Put(_methodTagKey, TagValue.Create(request.Method.ToString()))
                .Build();
        }

        protected internal string GetStatusCode(HttpResponseMessage response, TaskStatus taskStatus)
        {
            if (response != null)
            {
                int val = (int)response.StatusCode;
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

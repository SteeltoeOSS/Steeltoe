// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class OwinHostingObserver : MetricsObserver
    {
        internal const string STOP_EVENT = "Steeltoe.Owin.Hosting.HttpRequestIn.Stop";

        private const string OBSERVER_NAME = "OwinHostingObserver";
        private const string DIAGNOSTIC_NAME = "Steeltoe.Owin";

        private readonly ITagKey _statusTagKey = TagKey.Create("status");
        private readonly ITagKey _exceptionTagKey = TagKey.Create("exception");
        private readonly ITagKey _methodTagKey = TagKey.Create("method");
        private readonly ITagKey _uriTagKey = TagKey.Create("uri");

        private readonly IMeasureDouble _responseTimeMeasure;
        private readonly IMeasureLong _serverCountMeasure;

        public OwinHostingObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<OwinHostingObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            PathMatcher = new Regex(options.IngressIgnorePattern);

            _responseTimeMeasure = MeasureDouble.Create("server.owin.totalTime", "Total request time", MeasureUnit.MilliSeconds);
            _serverCountMeasure = MeasureLong.Create("server.owin.totalRequests", "Total request count", "count");

            var view = View.Create(
                    ViewName.Create("http.server.request.time"),
                    "Total request time",
                    _responseTimeMeasure,
                    Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
                    new List<ITagKey>() { _statusTagKey, _exceptionTagKey, _methodTagKey, _uriTagKey });

            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("http.server.request.count"),
                    "Total request counts",
                    _serverCountMeasure,
                    Sum.Create(),
                    new List<ITagKey>() { _statusTagKey, _exceptionTagKey, _methodTagKey, _uriTagKey });

            ViewManager.RegisterView(view);
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

            if (evnt == STOP_EVENT)
            {
                Logger?.LogTrace("HandleStopEvent start{thread}", Thread.CurrentThread.ManagedThreadId);

                var context = DiagnosticHelpers.GetProperty<IOwinContext>(arg, "OwinContext");

                if (context != null)
                {
                    HandleStopEvent(current, context);
                }

                Logger?.LogTrace("HandleStopEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleStopEvent(Activity current, IOwinContext arg)
        {
            if (ShouldIgnoreRequest(arg.Request.Path.Value))
            {
                Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", arg.Request.Path.Value);
                return;
            }

            if (current.Duration.TotalMilliseconds > 0)
            {
                var tagContext = GetTagContext(arg);
                StatsRecorder
                    .NewMeasureMap()
                    .Put(_responseTimeMeasure, current.Duration.TotalMilliseconds)
                    .Put(_serverCountMeasure, 1)
                    .Record(tagContext);
            }
        }

        protected internal ITagContext GetTagContext(IOwinContext arg)
        {
            var uri = arg.Request.Path.ToString();
            var statusCode = arg.Response.StatusCode.ToString();
            var exception = GetException(arg);

            return Tagger
                .EmptyBuilder
                .Put(_uriTagKey, TagValue.Create(uri))
                .Put(_statusTagKey, TagValue.Create(statusCode))
                .Put(_exceptionTagKey, TagValue.Create(exception))
                .Put(_methodTagKey, TagValue.Create(arg.Request.Method))
                .Build();
        }

        protected internal string GetException(IOwinContext arg)
        {
            var exception = arg.Get<Exception>("Steeltoe.Owin.Exception");
            if (exception != null)
            {
                return exception.GetType().Name;
            }

            return "None";
        }
    }
}

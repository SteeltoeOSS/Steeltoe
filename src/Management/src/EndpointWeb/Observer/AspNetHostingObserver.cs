// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class AspNetHostingObserver : MetricsObserver
    {
        internal const string DIAGNOSTIC_NAME = "Microsoft.AspNet.TelemetryCorrelation";
        internal const string OBSERVER_NAME = "AspNetWebObserver";
        internal const string STOP_EVENT = "Microsoft.AspNet.HttpReqIn.Stop";
        internal const string STOP_EVENT_ACTIVITY_LOST = "Microsoft.AspNet.HttpReqIn.ActivityLost.Stop";
        internal const string STOP_EVENT_ACTIVITY_RESTORED = "Microsoft.AspNet.HttpReqIn.ActivityRestored.Stop";

        private readonly ITagKey _statusTagKey = TagKey.Create("status");
        private readonly ITagKey _exceptionTagKey = TagKey.Create("exception");
        private readonly ITagKey _methodTagKey = TagKey.Create("method");
        private readonly ITagKey _uriTagKey = TagKey.Create("uri");

        private readonly IMeasureDouble _responseTimeMeasure;
        private readonly IMeasureLong _serverCountMeasure;

        public AspNetHostingObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<AspNetHostingObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            PathMatcher = new Regex(options.IngressIgnorePattern);

            _responseTimeMeasure = MeasureDouble.Create("server.core.totalTime", "Total request time", MeasureUnit.MilliSeconds);
            _serverCountMeasure = MeasureLong.Create("server.core.totalRequests", "Total request count", "count");

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
            if (evnt == STOP_EVENT)
            {
                Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                Activity current = Activity.Current;
                if (current != null)
                {
                    var context = HttpContext.Current;

                    if (context != null)
                    {
                        HandleStopEvent(current, context);
                    }
                }

                Logger?.LogTrace("HandleStopEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == STOP_EVENT_ACTIVITY_LOST)
            {
                Logger?.LogTrace("HandleStopEventLost start {thread}", Thread.CurrentThread.ManagedThreadId);

                Activity current = Activity.Current ?? DiagnosticHelpers.GetProperty<Activity>(arg, "activity");
                if (current != null)
                {
                    var context = HttpContext.Current;

                    if (context != null)
                    {
                        HandleStopEvent(current, context);
                    }
                }

                Logger?.LogTrace("HandleStopEventLost finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == STOP_EVENT_ACTIVITY_RESTORED)
            {
                Logger?.LogTrace("HandleStopEventRestored start{thread}", Thread.CurrentThread.ManagedThreadId);

                if (arg != null)
                {
                    Activity current = DiagnosticHelpers.GetProperty<Activity>(arg, "Activity");
                    if (current != null)
                    {
                        var context = HttpContext.Current;

                        if (context != null)
                        {
                            HandleStopEvent(current, context);
                        }
                    }
                }

                Logger?.LogTrace("HandleStopEventRestored finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleStopEvent(Activity current, HttpContext arg)
        {
            if (ShouldIgnoreRequest(arg.Request.Path))
            {
                Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", arg.Request.Path);
                return;
            }

            // attempt to calculate a duration if a start time is provided
            TimeSpan duration = current.Duration;
            if (current.Duration.Ticks == 0)
            {
                duration = DateTime.UtcNow - current.StartTimeUtc;
            }

            if (duration.TotalMilliseconds > 0)
            {
                ITagContext tagContext = GetTagContext(arg);
                StatsRecorder
                    .NewMeasureMap()
                    .Put(_responseTimeMeasure, duration.TotalMilliseconds)
                    .Put(_serverCountMeasure, 1)
                    .Record(tagContext);
            }
        }

        protected internal ITagContext GetTagContext(HttpContext arg)
        {
            var uri = arg.Request.Path.ToString();
            var statusCode = arg.Response.StatusCode.ToString();
            var exception = GetException(arg);

            return Tagger
                .EmptyBuilder
                .Put(_uriTagKey, TagValue.Create(uri))
                .Put(_statusTagKey, TagValue.Create(statusCode))
                .Put(_exceptionTagKey, TagValue.Create(exception))
                .Put(_methodTagKey, TagValue.Create(arg.Request.HttpMethod))
                .Build();
        }

        protected internal string GetException(HttpContext arg)
        {
            var errors = arg.AllErrors;

            if (errors != null && errors.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exp in errors)
                {
                    sb.Append(exp.GetType().Name);
                    sb.Append(",");
                }

                return sb.ToString(0, sb.Length - 1);
            }

            return "None";
        }
    }
}

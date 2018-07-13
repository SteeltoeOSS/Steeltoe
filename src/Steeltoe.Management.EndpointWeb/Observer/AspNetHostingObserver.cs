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

        private readonly ITagKey statusTagKey = TagKey.Create("status");
        private readonly ITagKey exceptionTagKey = TagKey.Create("exception");
        private readonly ITagKey methodTagKey = TagKey.Create("method");
        private readonly ITagKey uriTagKey = TagKey.Create("uri");

        private readonly IMeasureDouble responseTimeMeasure;
        private readonly IMeasureLong serverCountMeasure;

        public AspNetHostingObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<AspNetHostingObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            PathMatcher = new Regex(options.IngressIgnorePattern);

            responseTimeMeasure = MeasureDouble.Create("server.core.totalTime", "Total request time", MeasureUnit.MilliSeconds);
            serverCountMeasure = MeasureLong.Create("server.core.totalRequests", "Total request count", "count");

            var view = View.Create(
                    ViewName.Create("http.server.requests"),
                    "Total request time",
                    responseTimeMeasure,
                    Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
                    new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("http.server.requests.count"),
                    "Total request counts",
                    serverCountMeasure,
                    Sum.Create(),
                    new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

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
                    .Put(responseTimeMeasure, duration.TotalMilliseconds)
                    .Put(serverCountMeasure, 1)
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
                .Put(uriTagKey, TagValue.Create(uri))
                .Put(statusTagKey, TagValue.Create(statusCode))
                .Put(exceptionTagKey, TagValue.Create(exception))
                .Put(methodTagKey, TagValue.Create(arg.Request.HttpMethod))
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

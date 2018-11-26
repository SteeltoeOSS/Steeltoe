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

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Common.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class AspNetCoreHostingObserver : MetricsObserver
    {
        internal const string STOP_EVENT = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

        private const string OBSERVER_NAME = "AspNetCoreHostingObserver";
        private const string DIAGNOSTIC_NAME = "Microsoft.AspNetCore";

        private readonly ITagKey statusTagKey = TagKey.Create("status");
        private readonly ITagKey exceptionTagKey = TagKey.Create("exception");
        private readonly ITagKey methodTagKey = TagKey.Create("method");
        private readonly ITagKey uriTagKey = TagKey.Create("uri");

        private readonly IMeasureDouble responseTimeMeasure;
        private readonly IMeasureLong serverCountMeasure;

        public AspNetCoreHostingObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<AspNetCoreHostingObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            PathMatcher = new Regex(options.IngressIgnorePattern);

            responseTimeMeasure = MeasureDouble.Create("server.core.totalTime", "Total request time", MeasureUnit.MilliSeconds);
            serverCountMeasure = MeasureLong.Create("server.core.totalRequests", "Total request count", "count");

            var view = View.Create(
                    ViewName.Create("http.server.request.time"),
                    "Total request time",
                    responseTimeMeasure,
                    Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
                    new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

            ViewManager.RegisterView(view);

            view = View.Create(
                    ViewName.Create("http.server.request.count"),
                    "Total request counts",
                    serverCountMeasure,
                    Sum.Create(),
                    new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

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

            if (evnt == STOP_EVENT)
            {
                Logger?.LogTrace("HandleStopEvent start{thread}", Thread.CurrentThread.ManagedThreadId);

                var context = DiagnosticHelpers.GetProperty<HttpContext>(arg, "HttpContext");

                if (context != null)
                {
                    HandleStopEvent(current, context);
                }

                Logger?.LogTrace("HandleStopEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleStopEvent(Activity current, HttpContext arg)
        {
            if (ShouldIgnoreRequest(arg.Request.Path))
            {
                Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", arg.Request.Path);
                return;
            }

            if (current.Duration.TotalMilliseconds > 0)
            {
                ITagContext tagContext = GetTagContext(arg);
                StatsRecorder
                    .NewMeasureMap()
                    .Put(responseTimeMeasure, current.Duration.TotalMilliseconds)
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
                .Put(methodTagKey, TagValue.Create(arg.Request.Method))
                .Build();
        }

        protected internal string GetException(HttpContext arg)
        {
            var exception = arg.Features.Get<IExceptionHandlerFeature>();
            if (exception != null && exception.Error != null)
            {
                return exception.Error.GetType().Name;
            }

            return "None";
        }
    }
}

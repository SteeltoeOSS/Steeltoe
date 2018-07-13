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
using Microsoft.Owin;
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
    public class OwinHostingObserver : MetricsObserver
    {
        internal const string STOP_EVENT = "Steeltoe.Owin.Hosting.HttpRequestIn.Stop";

        private const string OBSERVER_NAME = "OwinHostingObserver";
        private const string DIAGNOSTIC_NAME = "Steeltoe.Owin";

        private readonly ITagKey statusTagKey = TagKey.Create("status");
        private readonly ITagKey exceptionTagKey = TagKey.Create("exception");
        private readonly ITagKey methodTagKey = TagKey.Create("method");
        private readonly ITagKey uriTagKey = TagKey.Create("uri");

        private readonly IMeasureDouble responseTimeMeasure;
        private readonly IMeasureLong serverCountMeasure;

        public OwinHostingObserver(IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger<OwinHostingObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, censusStats, censusTags, logger)
        {
            PathMatcher = new Regex(options.IngressIgnorePattern);

            responseTimeMeasure = MeasureDouble.Create("server.owin.totalTime", "Total request time", MeasureUnit.MilliSeconds);
            serverCountMeasure = MeasureLong.Create("server.owin.totalRequests", "Total request count", "count");

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
                ITagContext tagContext = GetTagContext(arg);
                StatsRecorder
                    .NewMeasureMap()
                    .Put(responseTimeMeasure, current.Duration.TotalMilliseconds)
                    .Put(serverCountMeasure, 1)
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
                .Put(uriTagKey, TagValue.Create(uri))
                .Put(statusTagKey, TagValue.Create(statusCode))
                .Put(exceptionTagKey, TagValue.Create(exception))
                .Put(methodTagKey, TagValue.Create(arg.Request.Method))
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

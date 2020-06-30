// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Stats;
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

        private readonly string statusTagKey = "status";
        private readonly string exceptionTagKey = "exception";
        private readonly string methodTagKey = "method";
        private readonly string uriTagKey = "uri";

        private readonly MeasureMetric<double> responseTimeMeasure;
        private readonly CounterMetric<long> serverCountMeasure;

        public AspNetCoreHostingObserver(IMetricsObserverOptions options, IStats stats, ILogger<AspNetCoreHostingObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, stats, logger)
        {
            PathMatcher = new Regex(options.IngressIgnorePattern);

            this.responseTimeMeasure = Meter.CreateDoubleMeasure("http.server.request.time");
            this.serverCountMeasure = Meter.CreateInt64Counter("http.server.request.count");
            /*
            //var view = View.Create(
            //        ViewName.Create("http.server.request.time"),
            //        "Total request time",
            //        responseTimeMeasure,
            //        Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
            //        new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

            //ViewManager.RegisterView(view);

            //view = View.Create(
            //        ViewName.Create("http.server.request.count"),
            //        "Total request counts",
            //        serverCountMeasure,
            //        Sum.Create(),
            //        new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

            //ViewManager.RegisterView(view);
            */
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
                var labelSets = GetLabelSets(arg); // Todo: Used bound labelsets

                serverCountMeasure.Add(default(SpanContext), 1, labelSets);
                labelSets.Add(new KeyValuePair<string, string>("TimeUnit", "ms"));
                responseTimeMeasure.Record(default(SpanContext), current.Duration.TotalMilliseconds, labelSets);
            }
        }

        protected internal List<KeyValuePair<string, string>> GetLabelSets(HttpContext arg)
        {
            var uri = arg.Request.Path.ToString();
            var statusCode = arg.Response.StatusCode.ToString();
            var exception = GetException(arg);

            var tagValues = new List<KeyValuePair<string, string>>();
            tagValues.Add(new KeyValuePair<string, string>(uriTagKey, uri));
            tagValues.Add(new KeyValuePair<string, string>(statusTagKey, statusCode));
            tagValues.Add(new KeyValuePair<string, string>(exceptionTagKey, exception));
            tagValues.Add(new KeyValuePair<string, string>(methodTagKey, arg.Request.Method));

            return tagValues;
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

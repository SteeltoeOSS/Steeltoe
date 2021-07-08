// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class AspNetCoreHostingObserver : MetricsObserver
    {
        internal const string STOP_EVENT = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

        private const string OBSERVER_NAME = "AspNetCoreHostingObserver";
        private const string DIAGNOSTIC_NAME = "Microsoft.AspNetCore";

        private readonly string _statusTagKey = "status";
        private readonly string _exceptionTagKey = "exception";
        private readonly string _methodTagKey = "method";
        private readonly string _uriTagKey = "uri";

        private readonly MeasureMetric<double> _responseTimeMeasure;
        private readonly CounterMetric<long> _serverCountMeasure;

        public AspNetCoreHostingObserver(IMetricsObserverOptions options, IStats stats, ILogger<AspNetCoreHostingObserver> logger)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, stats, logger)
        {
            PathMatcher = new Regex(options.IngressIgnorePattern);

            _responseTimeMeasure = Meter.CreateDoubleMeasure("http.server.requests.seconds");
            _serverCountMeasure = Meter.CreateInt64Counter("http.server.requests.count");
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

            var current = Activity.Current;
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

                _serverCountMeasure.Add(default, 1, labelSets);
                _responseTimeMeasure.Record(default, current.Duration.TotalSeconds, labelSets);
            }
        }

        protected internal List<KeyValuePair<string, string>> GetLabelSets(HttpContext arg)
        {
            var uri = arg.Request.Path.ToString();
            var statusCode = arg.Response.StatusCode.ToString();
            var exception = GetException(arg);

            var tagValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(_uriTagKey, uri),
                new KeyValuePair<string, string>(_statusTagKey, statusCode),
                new KeyValuePair<string, string>(_exceptionTagKey, exception),
                new KeyValuePair<string, string>(_methodTagKey, arg.Request.Method)
            };

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

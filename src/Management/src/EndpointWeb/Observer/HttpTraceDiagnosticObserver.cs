// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Steeltoe.Management.Endpoint.Trace.Observer
{
    public class HttpTraceDiagnosticObserver : DiagnosticObserver, IHttpTraceRepository
    {
        internal ConcurrentQueue<HttpTrace> _queue = new ConcurrentQueue<HttpTrace>();

        private const string DIAGNOSTIC_NAME = "Microsoft.AspNet.TelemetryCorrelation";
        private const string OBSERVER_NAME = "HttpTraceDiagnosticObserver";
        private const string STOP_EVENT = "Microsoft.AspNet.HttpReqIn.Stop";
        private const string STOP_EVENT_ACTIVITY_LOST = "Microsoft.AspNet.HttpReqIn.ActivityLost.Stop";
        private const string STOP_EVENT_ACTIVITY_RESTORED = "Microsoft.AspNet.HttpReqIn.ActivityRestored.Stop";

        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private ILogger<HttpTraceDiagnosticObserver> _logger;
        private ITraceOptions _options;

        public HttpTraceDiagnosticObserver(ITraceOptions options, ILogger<HttpTraceDiagnosticObserver> logger = null)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public HttpTraceResult GetTraces()
        {
            return new HttpTraceResult(_queue.ToList());
        }

        public override void ProcessEvent(string key, object value)
        {
            Activity current = null;

            if (key == STOP_EVENT)
            {
                current = Activity.Current;
            }
            else if (key == STOP_EVENT_ACTIVITY_RESTORED)
            {
                current = DiagnosticHelpers.GetProperty<Activity>(value, "Activity");
            }
            else if (key == STOP_EVENT_ACTIVITY_LOST)
            {
                current = DiagnosticHelpers.GetProperty<Activity>(value, "activity");
            }

            if (current == null)
            {
                return;
            }

            var context = HttpContext.Current;

            if (context != null)
            {
                var duration = current.Duration;
                if (duration.Ticks == 0)
                {
                    duration = DateTime.UtcNow - current.StartTimeUtc;
                }

                var trace = MakeTrace(context, duration);
                _queue.Enqueue(trace);

                if (_queue.Count > _options.Capacity && !_queue.TryDequeue(out _))
                {
                    _logger?.LogDebug("Stop - Dequeue failed");
                }
            }
        }

        protected internal HttpTrace MakeTrace(HttpContext context, TimeSpan duration)
        {
            var req = context.Request;
            var res = context.Response;

            var request = new Request(req.HttpMethod, req.Url.ToString(), GetHeaders(req.Headers), GetRemoteAddress(context));
            var response = new Response(res.StatusCode, GetHeaders(res.Headers));
            var principal = new Principal(context.User?.Identity?.Name);
            var session = new Session(context.Session?.SessionID);
            return new HttpTrace(request, response, GetJavaTime(DateTime.Now.Ticks), principal, session, duration.TotalMilliseconds);
        }

        protected internal Dictionary<string, string[]> GetHeaders(NameValueCollection headers)
        {
            var result = new Dictionary<string, string[]>();
            foreach (var h in headers.AllKeys)
            {
                result.Add(h.ToLowerInvariant(), headers.GetValues(h));
            }

            return result;
        }

        protected internal string GetRemoteAddress(HttpContext context)
        {
            return context?.Request?.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? context?.Request?.ServerVariables["REMOTE_ADDR"];
        }

        protected internal long GetJavaTime(long ticks)
        {
            var javaTicks = ticks - baseTime.Ticks;
            return javaTicks / 10000;
        }
    }
}

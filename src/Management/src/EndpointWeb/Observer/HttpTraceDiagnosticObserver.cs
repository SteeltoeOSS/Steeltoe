// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        private static readonly DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly ILogger<HttpTraceDiagnosticObserver> _logger;
        private readonly ITraceOptions _options;

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

            HttpContext context = HttpContext.Current;

            if (context != null)
            {
                TimeSpan duration = current.Duration;
                if (duration.Ticks == 0)
                {
                    duration = DateTime.UtcNow - current.StartTimeUtc;
                }

                HttpTrace trace = MakeTrace(context, duration);
                _queue.Enqueue(trace);

                if (_queue.Count > _options.Capacity)
                {
                    if (!_queue.TryDequeue(out _))
                    {
                        _logger?.LogDebug("Stop - Dequeue failed");
                    }
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
            long javaTicks = ticks - BaseTime.Ticks;
            return javaTicks / 10000;
        }

        // private bool HasFormContentType(HttpRequest request)
        // {
        //     return request.ContentType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) || request.ContentType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);
        // }
    }
}

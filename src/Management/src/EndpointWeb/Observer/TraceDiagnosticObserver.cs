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
using System.Web;

namespace Steeltoe.Management.Endpoint.Trace.Observer
{
    public class TraceDiagnosticObserver : DiagnosticObserver, ITraceRepository
    {
        internal ConcurrentQueue<TraceResult> _queue = new ConcurrentQueue<TraceResult>();

        private const string DIAGNOSTIC_NAME = "Microsoft.AspNet.TelemetryCorrelation";
        private const string OBSERVER_NAME = "TraceDiagnosticObserver";
        private const string STOP_EVENT = "Microsoft.AspNet.HttpReqIn.Stop";
        private const string STOP_EVENT_ACTIVITY_LOST = "Microsoft.AspNet.HttpReqIn.ActivityLost.Stop";
        private const string STOP_EVENT_ACTIVITY_RESTORED = "Microsoft.AspNet.HttpReqIn.ActivityRestored.Stop";

        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private ILogger<TraceDiagnosticObserver> _logger;
        private ITraceOptions _options;

        public TraceDiagnosticObserver(ITraceOptions options, ILogger<TraceDiagnosticObserver> logger = null)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public List<TraceResult> GetTraces()
        {
            TraceResult[] traces = _queue.ToArray();
            return new List<TraceResult>(traces);
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

                TraceResult trace = MakeTrace(context, duration);
                _queue.Enqueue(trace);

                if (_queue.Count > _options.Capacity && !_queue.TryDequeue(out _))
                {
                    _logger?.LogDebug("Stop - Dequeue failed");
                }
            }
        }

        protected internal TraceResult MakeTrace(HttpContext context, TimeSpan duration)
        {
            var request = context.Request;
            var response = context.Response;

            Dictionary<string, object> details = new Dictionary<string, object>
            {
                { "method", request.HttpMethod },
                { "path", GetPathInfo(request) }
            };

            Dictionary<string, object> headers = new Dictionary<string, object>();
            details.Add("headers", headers);

            if (_options.AddRequestHeaders)
            {
                headers.Add("request", GetRequestHeaders(request.Headers));
            }

            if (_options.AddResponseHeaders)
            {
                headers.Add("response", GetResponseHeaders(response.StatusCode, response.Headers));
            }

            if (_options.AddPathInfo)
            {
                details.Add("pathInfo", GetPathInfo(request));
            }

            if (_options.AddUserPrincipal)
            {
                details.Add("userPrincipal", context.User?.Identity?.Name);
            }

            if (_options.AddParameters)
            {
                details.Add("parameters", GetRequestParameters(request));
            }

            if (_options.AddQueryString)
            {
                details.Add("query", request.Url.Query);
            }

            if (_options.AddAuthType)
            {
                details.Add("authType", context.User?.Identity?.AuthenticationType);
            }

            if (_options.AddRemoteAddress)
            {
                details.Add("remoteAddress", GetRemoteAddress(context));
            }

            if (_options.AddSessionId)
            {
                details.Add("sessionId", context.Session?.SessionID);
            }

            if (_options.AddTimeTaken)
            {
                details.Add("timeTaken", GetTimeTaken(duration));
            }

            return new TraceResult(GetJavaTime(DateTime.Now.Ticks), details);
        }

        protected internal string GetPathInfo(HttpRequest request)
        {
            return request.Path;
        }

        protected internal Dictionary<string, object> GetRequestHeaders(NameValueCollection headers)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var h in headers.AllKeys)
            {
                result.Add(h.ToLowerInvariant(), headers.GetValues(h));
            }

            return result;
        }

        protected internal Dictionary<string, object> GetResponseHeaders(int status, NameValueCollection headers)
        {
            var result = GetRequestHeaders(headers);
            result.Add("status", status.ToString());
            return result;
        }

        protected internal Dictionary<string, string[]> GetRequestParameters(HttpRequest request)
        {
            Dictionary<string, string[]> parameters = new Dictionary<string, string[]>();
            foreach (var p in request.QueryString.AllKeys)
            {
                parameters.Add(p, request.QueryString.GetValues(p));
            }

            if (HasFormContentType(request))
            {
                foreach (var p in request.Form.AllKeys)
                {
                    parameters.Add(p, request.Form.GetValues(p));
                }
            }

            return parameters;
        }

        protected internal string GetRemoteAddress(HttpContext context)
        {
            return context?.Request?.ServerVariables["HTTP_X_FORWARDED_FOR"] ?? context?.Request?.ServerVariables["REMOTE_ADDR"];
        }

        protected internal string GetTimeTaken(TimeSpan duration)
        {
            long timeInMilli = (long)duration.TotalMilliseconds;
            return timeInMilli.ToString();
        }

        protected internal long GetJavaTime(long ticks)
        {
            long javaTicks = ticks - baseTime.Ticks;
            return javaTicks / 10000;
        }

        private bool HasFormContentType(HttpRequest request)
        {
            return request.ContentType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) || request.ContentType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);
        }
    }
}

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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Diagnostics.Listeners;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Steeltoe.Management.Endpoint.Trace
{
    public class TraceDiagnosticObserver : DiagnosticObserver, ITraceRepository
    {
        internal ConcurrentQueue<Trace> _queue = new ConcurrentQueue<Trace>();

        private const string DIAGNOSTIC_NAME = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
        private const string STOP_EVENT = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private ILogger<TraceDiagnosticObserver> _logger;
        private ITraceOptions _options;

        public TraceDiagnosticObserver(ITraceOptions options, ILogger<TraceDiagnosticObserver> logger = null)
            : base(DIAGNOSTIC_NAME, logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public List<Trace> GetTraces()
        {
            Trace[] traces = _queue.ToArray();
            return new List<Trace>(traces);
        }

        public override void ProcessEvent(string key, object value)
        {
            if (!STOP_EVENT.Equals(key))
            {
                return;
            }

            Activity current = Activity.Current;
            if (current == null)
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            GetProperty(value, out HttpContext context);

            if (context != null)
            {
                Trace trace = MakeTrace(context, current.Duration);
                _queue.Enqueue(trace);

                if (_queue.Count > _options.Capacity)
                {
                    if (!_queue.TryDequeue(out Trace discard))
                    {
                        _logger?.LogDebug("Stop - Dequeue failed");
                    }
                }
            }
        }

        protected internal Trace MakeTrace(HttpContext context, TimeSpan duration)
        {
            var request = context.Request;
            var response = context.Response;

            Dictionary<string, object> details = new Dictionary<string, object>
            {
                { "method", request.Method },
                { "path", GetPathInfo(request) }
            };

            Dictionary<string, object> headers = new Dictionary<string, object>();
            details.Add("headers", headers);

            if (_options.AddRequestHeaders)
            {
                headers.Add("request", GetHeaders(request.Headers));
            }

            if (_options.AddResponseHeaders)
            {
                headers.Add("response", GetHeaders(response.StatusCode, response.Headers));
            }

            if (_options.AddPathInfo)
            {
                details.Add("pathInfo", GetPathInfo(request));
            }

            if (_options.AddUserPrincipal)
            {
                details.Add("userPrincipal", GetUserPrincipal(context));
            }

            if (_options.AddParameters)
            {
                details.Add("parameters", GetRequestParameters(request));
            }

            if (_options.AddQueryString)
            {
                details.Add("query", request.QueryString.Value);
            }

            if (_options.AddAuthType)
            {
                details.Add("authType", GetAuthType(request)); // TODO
            }

            if (_options.AddRemoteAddress)
            {
                details.Add("remoteAddress", GetRemoteAddress(context));
            }

            if (_options.AddSessionId)
            {
                details.Add("sessionId", GetSessionId(context));
            }

            if (_options.AddTimeTaken)
            {
                details.Add("timeTaken", GetTimeTaken(duration));
            }

            return new Trace(GetJavaTime(DateTime.Now.Ticks), details);
        }

        protected internal long GetJavaTime(long ticks)
        {
            long javaTicks = ticks - baseTime.Ticks;
            return javaTicks / 10000;
        }

        protected internal string GetSessionId(HttpContext context)
        {
            var sessionFeature = context.Features.Get<ISessionFeature>();
            return sessionFeature == null ? null : context.Session.Id;
        }

        protected internal string GetTimeTaken(TimeSpan duration)
        {
            long timeInMilli = (long)duration.TotalMilliseconds;
            return timeInMilli.ToString();
        }

        protected internal string GetAuthType(HttpRequest request)
        {
            return string.Empty;
        }

        protected internal Dictionary<string, string[]> GetRequestParameters(HttpRequest request)
        {
            Dictionary<string, string[]> parameters = new Dictionary<string, string[]>();
            var query = request.Query;
            foreach (var p in query)
            {
                parameters.Add(p.Key, p.Value.ToArray());
            }

            if (request.HasFormContentType && request.Form != null)
            {
                var formData = request.Form;
                foreach (var p in formData)
                {
                    parameters.Add(p.Key, p.Value.ToArray());
                }
            }

            return parameters;
        }

        protected internal string GetRequestUri(HttpRequest request)
        {
            return request.Scheme + "://" + request.Host.Value + request.Path.Value;
        }

        protected internal string GetPathInfo(HttpRequest request)
        {
            return request.Path.Value;
        }

        protected internal string GetUserPrincipal(HttpContext context)
        {
            return context?.User?.Identity?.Name;
        }

        protected internal string GetRemoteAddress(HttpContext context)
        {
            return context?.Connection?.RemoteIpAddress?.ToString();
        }

        protected internal Dictionary<string, object> GetHeaders(int status, IHeaderDictionary headers)
        {
            var result = GetHeaders(headers);
            result.Add("status", status.ToString());
            return result;
        }

        protected internal Dictionary<string, object> GetHeaders(IHeaderDictionary headers)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var h in headers)
            {
                // Add filtering
                result.Add(h.Key.ToLowerInvariant(), GetHeaderValue(h.Value));
            }

            return result;
        }

        protected internal object GetHeaderValue(StringValues values)
        {
            List<string> result = new List<string>();
            foreach (var v in values)
            {
                result.Add(v);
            }

            if (result.Count == 1)
            {
                return result[0];
            }

            if (result.Count == 0)
            {
                return string.Empty;
            }

            return result;
        }

        protected internal void GetProperty(object obj, out HttpContext context)
        {
            context = DiagnosticHelpers.GetProperty<HttpContext>(obj, "HttpContext");
        }
    }
}

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
using Microsoft.Extensions.Primitives;
using Microsoft.Owin;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Trace
{
    public class TraceDiagnosticObserver : DiagnosticObserver, ITraceRepository
    {
        internal const string STOP_EVENT = "Steeltoe.Owin.Hosting.HttpRequestIn.Stop";
        internal ConcurrentQueue<TraceResult> _queue = new ConcurrentQueue<TraceResult>();

        private const string OBSERVER_NAME = "HttpTraceDiagnosticObserver";
        private const string DIAGNOSTIC_NAME = "Steeltoe.Owin";
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

            GetProperty(value, out IOwinContext context);

            if (context != null)
            {
                TraceResult trace = MakeTrace(context, current.Duration);
                _queue.Enqueue(trace);
                if (_queue.Count > _options.Capacity && !_queue.TryDequeue(out _))
                {
                    _logger?.LogDebug("Stop - Dequeue failed");
                }
            }
        }

        protected internal TraceResult MakeTrace(IOwinContext context, TimeSpan duration)
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
                details.Add("userPrincipal", GetUserPrincipal(context));
            }

            if (_options.AddParameters)
            {
                details.Add("parameters", GetRequestParametersAsync(request));
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

            return new TraceResult(GetJavaTime(DateTime.Now.Ticks), details);
        }

        protected internal string GetPathInfo(IOwinRequest request)
        {
            return request.Path.Value;
        }

        protected internal Dictionary<string, object> GetRequestHeaders(IHeaderDictionary headers)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var h in headers)
            {
                // Add filtering
                result.Add(h.Key.ToLowerInvariant(), GetHeaderValue(h.Value));
            }

            return result;
        }

        protected internal Dictionary<string, object> GetResponseHeaders(int status, IHeaderDictionary headers)
        {
            var result = GetRequestHeaders(headers);
            result.Add("status", status.ToString());
            return result;
        }

        protected internal object GetHeaderValue(string[] values)
        {
            List<string> result = new List<string>();
            foreach (var v in values)
            {
                result.Add(v);
            }

            // if there's one result, return it
            if (result.Count == 1)
            {
                return result[0];
            }

            // return an empty string if we couldn't get a value out
            if (result.Count == 0)
            {
                return string.Empty;
            }

            return result;
        }

        protected internal string GetUserPrincipal(IOwinContext context)
        {
            return context?.Request?.User?.Identity?.Name;
        }

        protected internal async Task<Dictionary<string, string[]>> GetRequestParametersAsync(IOwinRequest request)
        {
            Dictionary<string, string[]> parameters = new Dictionary<string, string[]>();
            var query = request.Query;
            foreach (var p in query)
            {
                parameters.Add(p.Key, p.Value);
            }

            if (HasFormContentType(request))
            {
                var formData = await request.ReadFormAsync().ConfigureAwait(false);
                foreach (var p in formData)
                {
                    parameters.Add(p.Key, p.Value);
                }
            }

            return parameters;
        }

        protected internal string GetAuthType(IOwinRequest request)
        {
            return string.Empty;
        }

        protected internal string GetRemoteAddress(IOwinContext context)
        {
            return context?.Request?.RemoteIpAddress?.ToString();
        }

        protected internal string GetSessionId(IOwinContext context)
        {
            // REVIEW: accessing session in OWIN is... not this easy
            // var sessionFeature = context.Features.Get<ISessionFeature>();
            // return sessionFeature == null ? null : context.Session.Id;
            _logger?.LogInformation("SessionId requested, but this feature isn't implemented");
            return null;
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

        private bool HasFormContentType(IOwinRequest request)
        {
            return request.MediaType != null &&
                (request.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) || request.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase));
        }

        private void GetProperty(object obj, out IOwinContext context)
        {
            context = DiagnosticHelpers.GetProperty<IOwinContext>(obj, "OwinContext");
        }
    }
}

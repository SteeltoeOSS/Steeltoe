// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Trace
{
    public class HttpTraceDiagnosticObserver : DiagnosticObserver, IHttpTraceRepository
    {
        internal ConcurrentQueue<HttpTrace> _queue = new ConcurrentQueue<HttpTrace>();

        private const string DIAGNOSTIC_NAME = "Microsoft.AspNetCore";
        private const string OBSERVER_NAME = "HttpTraceDiagnosticObserver";
        private const string STOP_EVENT = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

        private static readonly DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly ILogger<TraceDiagnosticObserver> _logger;
        private readonly ITraceOptions _options;

        public HttpTraceDiagnosticObserver(ITraceOptions options, ILogger<TraceDiagnosticObserver> logger = null)
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
            if (!STOP_EVENT.Equals(key))
            {
                return;
            }

            var current = Activity.Current;
            if (current == null)
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            GetProperty(value, out var context);

            if (context != null)
            {
                var trace = MakeTrace(context, current.Duration);
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

            var request = new Request(req.Method, GetRequestUri(req), GetHeaders(req.Headers), GetRemoteAddress(context));
            var response = new Response(res.StatusCode, GetHeaders(res.Headers));
            var principal = new Principal(GetUserPrincipal(context));
            var session = new Session(GetSessionId(context));
            return new HttpTrace(request, response, GetJavaTime(DateTime.Now.Ticks), principal, session, duration.Milliseconds);
        }

        protected internal long GetJavaTime(long ticks)
        {
            var javaTicks = ticks - BaseTime.Ticks;
            return javaTicks / 10000;
        }

        protected internal string GetSessionId(HttpContext context)
        {
            var sessionFeature = context.Features.Get<ISessionFeature>();
            return sessionFeature == null ? null : context.Session.Id;
        }

        protected internal string GetTimeTaken(TimeSpan duration)
        {
            var timeInMilli = (long)duration.TotalMilliseconds;
            return timeInMilli.ToString();
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

        protected internal Dictionary<string, string[]> GetHeaders(IHeaderDictionary headers)
        {
            var result = new Dictionary<string, string[]>();
            foreach (var h in headers)
            {
                // Add filtering
                result.Add(h.Key.ToLowerInvariant(), h.Value.ToArray());
            }

            return result;
        }

        protected internal void GetProperty(object obj, out HttpContext context)
        {
            context = DiagnosticHelpers.GetProperty<HttpContext>(obj, "HttpContext");
        }
    }
}

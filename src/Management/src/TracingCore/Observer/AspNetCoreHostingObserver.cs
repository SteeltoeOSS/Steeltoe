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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using OpenCensus.Common;
using OpenCensus.Trace;
using OpenCensus.Trace.Propagation;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Steeltoe.Management.Tracing.Observer
{
    public class AspNetCoreHostingObserver : AspNetCoreTracingObserver
    {
        internal const string HOSTING_STOP_EVENT = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";
        internal const string HOSTING_START_EVENT = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";
        internal const string HOSTING_EXCEPTION_EVENT = "Microsoft.AspNetCore.Hosting.UnhandledException";
        internal const string DIAG_HANDLEDEXCEPTION_EVENT = "Microsoft.AspNetCore.Diagnostics.HandledException";
        internal const string DIAG_UNHANDLEDEXCEPTION_EVENT = "Microsoft.AspNetCore.Diagnostics.UnhandledException";

        private const string OBSERVER_NAME = "AspNetCoreHostingDiagnosticObserver";

        private static AsyncLocal<SpanContext> active = new AsyncLocal<SpanContext>();

        public AspNetCoreHostingObserver(ITracingOptions options, ITracing tracing, ILogger<AspNetCoreHostingObserver> logger = null)
            : base(OBSERVER_NAME, options, tracing, logger)
        {
        }

        protected internal SpanContext Active
        {
            get
            {
                return active.Value;
            }
        }

        public override void ProcessEvent(string evnt, object arg)
        {
            if (arg == null)
            {
                return;
            }

            if (evnt == HOSTING_START_EVENT)
            {
                Logger?.LogTrace("HandleStartEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                var context = DiagnosticHelpers.GetProperty<HttpContext>(arg, "HttpContext");

                if (context != null)
                {
                    HandleStartEvent(context);
                }

                Logger?.LogTrace("HandleStartEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == HOSTING_STOP_EVENT)
            {
                Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                var context = DiagnosticHelpers.GetProperty<HttpContext>(arg, "HttpContext");

                if (context != null)
                {
                    HandleStopEvent(context);
                }

                Logger?.LogTrace("HandleStopEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == HOSTING_EXCEPTION_EVENT ||
                    evnt == DIAG_UNHANDLEDEXCEPTION_EVENT ||
                    evnt == DIAG_HANDLEDEXCEPTION_EVENT)
            {
                Logger?.LogTrace("HandleExceptionEvent start{thread}", Thread.CurrentThread.ManagedThreadId);

                var context = DiagnosticHelpers.GetProperty<HttpContext>(arg, "httpContext");
                var exception = DiagnosticHelpers.GetProperty<Exception>(arg, "exception");
                if (context != null && exception != null)
                {
                    HandleExceptionEvent(context, exception);
                }

                Logger?.LogTrace("HandleExceptionEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleExceptionEvent(HttpContext context, Exception exception)
        {
            var spanContext = active.Value;
            if (spanContext == null)
            {
                Logger?.LogDebug("HandleExceptionEvent: Missing span context, {exception}", exception);
                return;
            }

            var span = spanContext.Active;
            if (span == null)
            {
                Logger?.LogDebug("HandleExceptionEvent: Active span missing, {exception}", exception);
                return;
            }

            span.PutErrorAttribute(GetExceptionMessage(exception))
                .PutErrorStackTraceAttribute(GetExceptionStackTrace(exception))
                .Status = Status.Aborted;
        }

        protected internal void HandleStartEvent(HttpContext context)
        {
            if (ShouldIgnoreRequest(context.Request.Path))
            {
                Logger?.LogDebug("HandleStartEvent: Ignoring path: {path}", context.Request.Path.ToString());
                return;
            }

            if (active.Value != null)
            {
                Logger?.LogDebug("HandleStartEvent: Continuing existing span!");
                return;
            }

            ISpanContext traceContext = ExtractTraceContext(context);
            string spanName = ExtractSpanName(context);

            ISpan span;
            IScope scope;
            if (traceContext != null)
            {
                Logger?.LogDebug("HandleStartEvent: Found parent span {parent}", traceContext.ToString());
                scope = Tracer.SpanBuilderWithRemoteParent(spanName, traceContext)
                    .StartScopedSpan(out span);
            }
            else
            {
                 scope = Tracer.SpanBuilder(spanName)
                    .StartScopedSpan(out span);
            }

            span.PutServerSpanKindAttribute()
                .PutHttpRawUrlAttribute(context.Request.GetDisplayUrl())
                .PutHttpMethodAttribute(context.Request.Method.ToString())
                .PutHttpPathAttribute(context.Request.Path.ToString())
                .PutHttpHostAttribute(context.Request.Host.Host, context.Request.Host.Port ?? 80);

            if (context.Request.Headers != null)
            {
                span.PutHttpRequestHeadersAttribute(AsList(context.Request.Headers));
            }

            active.Value = new SpanContext(span, scope);
        }

        protected internal void HandleStopEvent(HttpContext context)
        {
            var spanContext = active.Value;
            if (spanContext == null)
            {
                Logger?.LogDebug("HandleStopEvent: Missing span context");
                return;
            }

            ISpan span = spanContext.Active;
            IScope scope = spanContext.ActiveScope;

            span.PutHttpStatusCodeAttribute(context.Response.StatusCode);

            if (context.Response.Headers != null)
            {
                span.PutHttpResponseHeadersAttribute(AsList(context.Response.Headers));
            }

            long? reqSize = ExtractRequestSize(context);
            if (reqSize != null)
            {
                span.PutHttpRequestSizeAttribute(reqSize.Value);
            }

            long? respSize = ExtractResponseSize(context);
            if (respSize != null)
            {
                span.PutHttpResponseSizeAttribute(respSize.Value);
            }

            scope.Dispose();
            active.Value = null;
        }

        protected internal string ExtractSpanName(HttpContext context)
        {
            return "http:" + context.Request.Path.Value;
        }

        protected internal ISpanContext ExtractTraceContext(HttpContext context)
        {
            var request = context.Request;
            try
            {
                return Propagation.Extract(request.Headers, (d, k) =>
                {
                    d.TryGetValue(k, out StringValues result);
                    return result;
                });
            }
            catch (SpanContextParseException)
            {
                // Ignore
            }

            return null;
        }

        protected internal virtual long? ExtractRequestSize(HttpContext context)
        {
            try
            {
                if (context.Request.Body != null)
                {
                    return context.Request.Body.Length;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        protected internal virtual long? ExtractResponseSize(HttpContext context)
        {
            try
            {
                if (context.Response.Body != null)
                {
                    return context.Response.Body.Length;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        protected internal List<KeyValuePair<string, IEnumerable<string>>> AsList(IHeaderDictionary headers)
        {
            List<KeyValuePair<string, IEnumerable<string>>> results = new List<KeyValuePair<string, IEnumerable<string>>>();
            foreach (var header in headers)
            {
                var enumerable = header.Value.AsEnumerable();
                results.Add(new KeyValuePair<string, IEnumerable<string>>(header.Key, enumerable));
            }

            return results;
        }
    }
}

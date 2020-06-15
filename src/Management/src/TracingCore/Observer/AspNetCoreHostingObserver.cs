// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Trace;
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

        private static readonly AsyncLocal<TelemetrySpan> ActiveValue = new AsyncLocal<TelemetrySpan>();

        public AspNetCoreHostingObserver(ITracingOptions options, ITracing tracing, ILogger<AspNetCoreHostingObserver> logger = null)
            : base(OBSERVER_NAME, options, tracing, logger)
        {
        }

        protected internal TelemetrySpan Active
        {
            get
            {
                return ActiveValue.Value;
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
            var span = ActiveValue.Value;
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

            if (ActiveValue.Value != null)
            {
                Logger?.LogDebug("HandleStartEvent: Continuing existing span!");
                return;
            }

            var traceContext = ExtractTraceContext(context);
            string spanName = ExtractSpanName(context);

            TelemetrySpan span;
            if (!traceContext.IsValid)
            {
                Logger?.LogDebug("HandleStartEvent: Found parent span {parent}", traceContext.ToString());
                Tracer.StartActiveSpan(spanName, traceContext, SpanKind.Server, out span);
            }
            else
            {
                Tracer.StartActiveSpan(spanName, SpanKind.Server, out span);
            }

            span.PutHttpRawUrlAttribute(context.Request.GetDisplayUrl())
                .PutHttpMethodAttribute(context.Request.Method.ToString())
                .PutHttpPathAttribute(context.Request.Path.ToString())
                .PutHttpHostAttribute(context.Request.Host.Host, context.Request.Host.Port ?? 80);

            if (context.Request.Headers != null)
            {
                span.PutHttpRequestHeadersAttribute(AsList(context.Request.Headers));
            }

            ActiveValue.Value = span;
        }

        protected internal void HandleStopEvent(HttpContext context)
        {
            var span = ActiveValue.Value;
            if (span == null)
            {
                Logger?.LogDebug("HandleStopEvent: Missing span");
                return;
            }

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

            span.End();
            ActiveValue.Value = null;
        }

        protected internal string ExtractSpanName(HttpContext context)
        {
            return "http:" + context.Request.Path.Value;
        }

        protected internal SpanContext ExtractTraceContext(HttpContext context)
        {
            var request = context.Request;
            return Propagation.Extract(request.Headers, (d, k) =>
                 {
                     d.TryGetValue(k, out var result);
                     return result;
                 });
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
                // code flow can continue just fine if the above fails for any reason
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
                // code flow can continue just fine if the above fails for any reason
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

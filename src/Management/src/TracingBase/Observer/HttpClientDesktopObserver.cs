﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace.Propagation;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Steeltoe.Management.Tracing.Observer
{
    public class HttpClientDesktopObserver : HttpClientTracingObserver
    {
        internal const string START_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Start";
        internal const string STOP_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Stop";
        internal const string STOPEX_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";
        internal ConcurrentDictionary<HttpWebRequest, TelemetrySpan> Pending = new ConcurrentDictionary<HttpWebRequest, TelemetrySpan>();

        private const string DIAGNOSTIC_NAME = "System.Net.Http.Desktop";
        private const string OBSERVER_NAME = "HttpClientDesktopObserver";

        public HttpClientDesktopObserver(ITracingOptions options, ITracing tracing, ILogger<HttpClientDesktopObserver> logger = null)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, tracing, logger)
        {
        }

        public override void ProcessEvent(string evnt, object arg)
        {
            if (arg == null)
            {
                return;
            }

            var request = DiagnosticHelpers.GetProperty<HttpWebRequest>(arg, "Request");
            if (request == null)
            {
                return;
            }

            if (evnt == START_EVENT)
            {
                Logger?.LogTrace("HandleStartEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                HandleStartEvent(request);

                Logger?.LogTrace("HandleStartEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == STOP_EVENT)
            {
                Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                var response = DiagnosticHelpers.GetProperty<HttpWebResponse>(arg, "Response");
                if (response != null)
                {
                    HandleStopEvent(request, response.StatusCode, response.Headers);
                }

                Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == STOPEX_EVENT)
            {
                Logger?.LogTrace("HandleStopEventEx start {thread}", Thread.CurrentThread.ManagedThreadId);

                var statusCode = DiagnosticHelpers.GetProperty<HttpStatusCode>(arg, "StatusCode");
                var headers = DiagnosticHelpers.GetProperty<WebHeaderCollection>(arg, "Headers");

                HandleStopEvent(request, statusCode, headers);

                Logger?.LogTrace("HandleStopEventEx finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleStopEvent(HttpWebRequest request, HttpStatusCode statusCode, WebHeaderCollection headers)
        {
            if (!Pending.TryRemove(request, out var span))
            {
                Logger?.LogDebug("HandleStopEvent: Missing span context");
                return;
            }

            if (span != null)
            {
                span.PutHttpStatusCodeAttribute((int)statusCode);
                if (headers != null)
                {
                    span.PutHttpResponseHeadersAttribute(headers);
                }

                span.End();
            }
        }

        protected internal void HandleStartEvent(HttpWebRequest request)
        {
            if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
            {
                Logger?.LogDebug("HandleStartEvent: Ignoring path: {path}", request.RequestUri.AbsolutePath);
                return;
            }

            if (Pending.TryGetValue(request, out var span))
            {
                Logger?.LogDebug("HandleStartEvent: Continuing existing span!");
                return;
            }

            string spanName = ExtractSpanName(request);

            var parentSpan = GetCurrentSpan();

            TelemetrySpan started;
            if (parentSpan != null)
            {
                Tracer.StartActiveSpan(spanName, parentSpan, SpanKind.Client, out started);
            }
            else
            {
                Tracer.StartActiveSpan(spanName, SpanKind.Client, out started);
            }

            var existing = Pending.GetOrAdd(request, started);

            if (existing != started)
            {
                Logger?.LogWarning("Existing span context existed for web request");
            }

            started.PutHttpRawUrlAttribute(request.RequestUri.ToString())
                .PutHttpMethodAttribute(request.Method.ToString())
                .PutHttpHostAttribute(request.RequestUri.Host, request.RequestUri.Port)
                .PutHttpPathAttribute(request.RequestUri.AbsolutePath);

            if (request.Headers != null)
            {
                started.PutHttpRequestHeadersAttribute(request.Headers);
            }

            InjectTraceContext(request, parentSpan);
        }

        protected internal void InjectTraceContext(HttpWebRequest message, TelemetrySpan parentSpan)
        {
            // Expects the currentspan to be the span to inject into
            var headers = message.Headers;
            TextFormat.Inject(Tracer.CurrentSpan.Context, headers, (c, k, v) =>
            {
                if (k == B3Constants.XB3TraceId && v.Length > 16 && Options.UseShortTraceIds)
                {
                    v = v.Substring(v.Length - 16, 16);
                }

                if (c.Get(k) != null)
                {
                    c.Remove(k);
                }

                c.Add(k, v);
            });

            if (parentSpan != null)
            {
                headers.Add(B3Constants.XB3ParentSpanId, parentSpan.Context.SpanId.ToHexString());
            }
        }

        private string ExtractSpanName(HttpWebRequest message)
        {
            return "httpclient:" + message.RequestUri.AbsolutePath;
        }
    }
}

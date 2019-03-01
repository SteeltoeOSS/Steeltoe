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

using Microsoft.Extensions.Logging;
using OpenCensus.Common;
using OpenCensus.Trace;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Propagation;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Tracing.Observer
{
    public class HttpClientCoreObserver : HttpClientTracingObserver
    {
        internal const string DIAGNOSTIC_NAME = "HttpHandlerDiagnosticListener";
        internal const string OBSERVER_NAME = "HttpClientCoreObserver";

        internal const string START_EVENT = "System.Net.Http.HttpRequestOut.Start";
        internal const string STOP_EVENT = "System.Net.Http.HttpRequestOut.Stop";
        internal const string EXCEPTION_EVENT = "System.Net.Http.Exception";

        internal const string SPANCONTEXT_KEY = "Steeltoe.SpanContext";

        public HttpClientCoreObserver(ITracingOptions options, ITracing tracing, ILogger<HttpClientCoreObserver> logger = null)
            : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, tracing, logger)
        {
        }

        public override void ProcessEvent(string evnt, object arg)
        {
            if (arg == null)
            {
                return;
            }

            var request = DiagnosticHelpers.GetProperty<HttpRequestMessage>(arg, "Request");
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

                var response = DiagnosticHelpers.GetProperty<HttpResponseMessage>(arg, "Response");
                var requestStatus = DiagnosticHelpers.GetProperty<TaskStatus>(arg, "RequestTaskStatus");
                HandleStopEvent(request, response, requestStatus);

                Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
            else if (evnt == EXCEPTION_EVENT)
            {
                Logger?.LogTrace("HandleExceptionEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

                var exception = DiagnosticHelpers.GetProperty<Exception>(arg, "Exception");
                if (exception != null)
                {
                    HandleExceptionEvent(request, exception);
                }

                Logger?.LogTrace("HandleExceptionEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
            }
        }

        protected internal void HandleExceptionEvent(HttpRequestMessage request, Exception exception)
        {
            if (!request.Properties.TryGetValue(SPANCONTEXT_KEY, out object context))
            {
                Logger?.LogDebug("HandleExceptionEvent: Missing span context");
                return;
            }

            var spanContext = context as SpanContext;
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

        protected internal void HandleStartEvent(HttpRequestMessage request)
        {
            if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
            {
                Logger?.LogDebug("HandleStartEvent: Ignoring path: {path}", request.RequestUri.AbsolutePath);
                return;
            }

            if (request.Properties.TryGetValue(SPANCONTEXT_KEY, out object context))
            {
                Logger?.LogDebug("HandleStartEvent: Continuing existing span!");
                return;
            }

            string spanName = ExtractSpanName(request);

            var parentSpan = GetCurrentSpan();
            ISpan started;
            IScope scope;
            if (parentSpan != null)
            {
                scope = Tracer.SpanBuilderWithExplicitParent(spanName, parentSpan)
                    .StartScopedSpan(out started);
            }
            else
            {
                scope = Tracer.SpanBuilder(spanName)
                   .StartScopedSpan(out started);
            }

            request.Properties.Add(SPANCONTEXT_KEY, new SpanContext(started, scope));

            started.PutClientSpanKindAttribute()
                .PutHttpRawUrlAttribute(request.RequestUri.ToString())
                .PutHttpMethodAttribute(request.Method.ToString())
                .PutHttpHostAttribute(request.RequestUri.Host, request.RequestUri.Port)
                .PutHttpPathAttribute(request.RequestUri.AbsolutePath)
                .PutHttpRequestHeadersAttribute(request.Headers.ToList());

            InjectTraceContext(request, parentSpan);
        }

        protected internal void HandleStopEvent(HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
        {
            if (!request.Properties.TryGetValue(SPANCONTEXT_KEY, out object context))
            {
                Logger?.LogDebug("HandleStopEvent: Missing span context");
                return;
            }

            SpanContext spanContext = context as SpanContext;

            if (spanContext != null)
            {
                ISpan span = spanContext.Active;
                IScope scope = spanContext.ActiveScope;

                if (response != null)
                {
                    span.PutHttpStatusCodeAttribute((int)response.StatusCode)
                        .PutHttpResponseHeadersAttribute(response.Headers.ToList());
                }

                if (taskStatus == TaskStatus.Faulted)
                {
                    span.PutErrorAttribute("TaskStatus.Faulted")
                        .Status = Status.Aborted;
                }

                if (taskStatus == TaskStatus.Canceled)
                {
                    span.PutErrorAttribute("TaskStatus.Canceled")
                        .Status = Status.Cancelled;
                }

                scope.Dispose();  // Calls span.End(); and replaces CurrentSpan with previous span

                request.Properties.Remove(SPANCONTEXT_KEY);
            }
        }

        protected internal void InjectTraceContext(HttpRequestMessage message, ISpan parentSpan)
        {
            // Expects the currentspan to be the span to inject into
            var headers = message.Headers;
            Propagation.Inject(Tracer.CurrentSpan.Context, headers,  (c, k, v) =>
            {
                if (k == B3Constants.XB3TraceId)
                {
                    if (v.Length > 16 && Options.UseShortTraceIds)
                    {
                        v = v.Substring(v.Length - 16, 16);
                    }
                }
                c.Add(k, v);
            });
            if (parentSpan != null)
            {
                headers.Add(B3Constants.XB3ParentSpanId, parentSpan.Context.SpanId.ToLowerBase16());
            }
        }

        private string ExtractSpanName(HttpRequestMessage message)
        {
            return "httpclient:" + message.RequestUri.AbsolutePath;
        }
    }
}

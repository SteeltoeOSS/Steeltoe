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
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Propagation;
using Steeltoe.Management.Census.Trace.Unsafe;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;

namespace Steeltoe.Management.Tracing.Observer
{
    public class HttpClientDesktopObserver : HttpClientTracingObserver
    {
        internal const string START_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Start";
        internal const string STOP_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Stop";
        internal const string STOPEX_EVENT = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";
        internal ConcurrentDictionary<HttpWebRequest, SpanContext> Pending = new ConcurrentDictionary<HttpWebRequest, SpanContext>();

        private const string DIAGNOSTIC_NAME = "System.Net.Http.Desktop";
        private const string OBSERVER_NAME = "HttpClientDesktopObserver";

        private HeaderDictionarySetter headerSetter = new HeaderDictionarySetter();

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
            if (!Pending.TryRemove(request, out SpanContext spanContext))
            {
                Logger?.LogDebug("HandleStopEvent: Missing span context");
                return;
            }

            ISpan span = spanContext.Active;
            if (span != null)
            {
                span.PutHttpStatusCodeAttribute((int)statusCode);
                if (headers != null)
                {
                    span.PutHttpResponseHeadersAttribute(headers);
                }

                span.End();

                AsyncLocalContext.CurrentSpan = spanContext.Previous;
            }
        }

        protected internal void HandleStartEvent(HttpWebRequest request)
        {
            if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
            {
                Logger?.LogDebug("HandleStartEvent: Ignoring path: {path}", request.RequestUri.AbsolutePath);
                return;
            }

            if (Pending.TryGetValue(request, out SpanContext spanContext))
            {
                Logger?.LogDebug("HandleStartEvent: Continuing existing span!");
                return;
            }

            string spanName = ExtractSpanName(request);

            var parentSpan = AsyncLocalContext.CurrentSpan;
            ISpan started;
            if (parentSpan != null)
            {
                started = Tracer.SpanBuilderWithExplicitParent(spanName, parentSpan)
                    .StartSpan();
            }
            else
            {
                started = Tracer.SpanBuilder(spanName)
                    .StartSpan();
            }

            SpanContext existing = Pending.GetOrAdd(request, new SpanContext(started, parentSpan));

            if (existing != started)
            {
                Logger?.LogWarning("Existing span context existed for web request");
            }

            started.PutClientSpanKindAttribute()
                .PutHttpUrlAttribute(request.RequestUri.ToString())
                .PutHttpMethodAttribute(request.Method.ToString())
                .PutHttpHostAttribute(request.RequestUri.Host)
                .PutHttpPathAttribute(request.RequestUri.AbsolutePath);

            if (request.Headers != null)
            {
                started.PutHttpRequestHeadersAttribute(request.Headers);
            }

            AsyncLocalContext.CurrentSpan = started;
            InjectTraceContext(request, parentSpan);
        }

        protected internal void InjectTraceContext(HttpWebRequest message, ISpan parentSpan)
        {
            var headers = message.Headers;

            var traceId = Tracer.CurrentSpan.Context.TraceId.ToLowerBase16();
            if (traceId.Length > 16 && Options.UseShortTraceIds)
            {
                traceId = traceId.Substring(traceId.Length - 16, 16);
            }

            headerSetter.Put(headers, B3Format.X_B3_TRACE_ID, traceId);
            headerSetter.Put(headers, B3Format.X_B3_SPAN_ID, Tracer.CurrentSpan.Context.SpanId.ToLowerBase16());
            if (Tracer.CurrentSpan.Context.TraceOptions.IsSampled)
            {
                headerSetter.Put(headers, B3Format.X_B3_SAMPLED, "1");
            }

            if (parentSpan != null)
            {
                headerSetter.Put(headers, B3Format.X_B3_PARENT_SPAN_ID, parentSpan.Context.SpanId.ToLowerBase16());
            }
        }

        private string ExtractSpanName(HttpWebRequest message)
        {
            return "httpclient:" + message.RequestUri.AbsolutePath;
        }

        public class HeaderDictionarySetter : ISetter<WebHeaderCollection>
        {
            public void Put(WebHeaderCollection carrier, string key, string value)
            {
                if (carrier.Get(key) != null)
                {
                    carrier.Remove(key);
                }

                carrier.Add(key, value);
            }
        }
    }
}

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

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Propagation;
using Steeltoe.Management.Census.Trace.Unsafe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class HttpClientCoreObserverTest
    {
        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresMissingHttpRequestMessage()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new object());
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { Request = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);

            Assert.True(obs.ShouldIgnoreRequest("/api/v2/spans"));
            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/foobar/permissions"));
            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/barfoo/permissions"));
            Assert.False(obs.ShouldIgnoreRequest("/api/test"));
            Assert.False(obs.ShouldIgnoreRequest("/v2/apps"));
        }

        [Fact]
        public void ProcessEvent_Stop_NothingStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request });
            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.Null(span);
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
        }

        [Fact]
        public void ProcessEvent_Exception_NothingStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            // No Exception
            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request });
            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.Null(span);
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));

            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request, Exception = new Exception() });
            span = AsyncLocalContext.CurrentSpan as Span;
            Assert.Null(span);
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out context));
        }

        [Fact]
        public void ProcessEvent_Exception_PreviousStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(span);
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
            HttpClientTracingObserver.SpanContext spanContext = context as HttpClientTracingObserver.SpanContext;
            Assert.NotNull(spanContext);
            Assert.Equal(span, spanContext.Active);
            Assert.Null(spanContext.Previous);
            Assert.Equal("httpclient:/", span.Name);

            var exception = new Exception("Help");
            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request, Exception = exception });

            var response = GetHttpResponseMessage(HttpStatusCode.InternalServerError);
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });
            Assert.True(span.HasEnded);
            Assert.Null(AsyncLocalContext.CurrentSpan);
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object ctx));

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(AttributeValue.StringAttributeValue(SpanAttributeConstants.ClientSpanKind), attributes[SpanAttributeConstants.SpanKindKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.response.TEST"]);
            Assert.Equal(AttributeValue.LongAttributeValue((long)HttpStatusCode.InternalServerError), attributes[SpanAttributeConstants.HttpStatusCodeKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(obs.GetExceptionMessage(exception)), attributes[SpanAttributeConstants.ErrorKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(obs.GetExceptionStackTrace(exception)), attributes[SpanAttributeConstants.ErrorStackTrace]);
        }

        [Fact]
        public void ProcessEvent_Stop_PreviousStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(span);
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
            HttpClientTracingObserver.SpanContext spanContext = context as HttpClientTracingObserver.SpanContext;
            Assert.NotNull(spanContext);
            Assert.Equal(span, spanContext.Active);
            Assert.Null(spanContext.Previous);
            Assert.Equal("httpclient:/", span.Name);

            var response = GetHttpResponseMessage(HttpStatusCode.OK);
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });
            Assert.True(span.HasEnded);
            Assert.Null(AsyncLocalContext.CurrentSpan);
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object ctx));

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(AttributeValue.StringAttributeValue(SpanAttributeConstants.ClientSpanKind), attributes[SpanAttributeConstants.SpanKindKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.response.TEST"]);
            Assert.Equal(AttributeValue.LongAttributeValue((long)HttpStatusCode.OK), attributes[SpanAttributeConstants.HttpStatusCodeKey]);
        }

        [Fact]
        public void ProcessEvent_Start()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(span);
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
            HttpClientTracingObserver.SpanContext spanContext = context as HttpClientTracingObserver.SpanContext;
            Assert.NotNull(spanContext);
            Assert.Equal(span, spanContext.Active);
            Assert.Null(spanContext.Previous);
            Assert.Equal("httpclient:/", span.Name);

            Assert.True(request.Headers.Contains(B3Format.X_B3_TRACE_ID));
            Assert.True(request.Headers.Contains(B3Format.X_B3_SPAN_ID));
            Assert.False(request.Headers.Contains(B3Format.X_B3_PARENT_SPAN_ID));

            var spanId = request.Headers.GetValues(B3Format.X_B3_SPAN_ID).Single();
            Assert.Equal(span.Context.SpanId.ToLowerBase16(), spanId);

            var traceId = request.Headers.GetValues(B3Format.X_B3_TRACE_ID).Single();
            var expected = GetTraceId(opts, span.Context);
            Assert.Equal(expected, traceId);

            if (span.Context.TraceOptions.IsSampled)
            {
                Assert.True(request.Headers.Contains(B3Format.X_B3_SAMPLED));
            }

            Assert.False(span.HasEnded);

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(AttributeValue.StringAttributeValue(SpanAttributeConstants.ClientSpanKind), attributes[SpanAttributeConstants.SpanKindKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
        }

        [Fact]
        public void InjectTraceContext()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            var span = tracing.Tracer.SpanBuilder("MySpan").StartSpan() as Span;
            AsyncLocalContext.CurrentSpan = span;

            obs.InjectTraceContext(request, null);

            Assert.True(request.Headers.Contains(B3Format.X_B3_TRACE_ID));
            Assert.True(request.Headers.Contains(B3Format.X_B3_SPAN_ID));
            Assert.False(request.Headers.Contains(B3Format.X_B3_PARENT_SPAN_ID));

            var spanId = request.Headers.GetValues(B3Format.X_B3_SPAN_ID).Single();
            Assert.Equal(span.Context.SpanId.ToLowerBase16(), spanId);

            var traceId = request.Headers.GetValues(B3Format.X_B3_TRACE_ID).Single();
            var expected = GetTraceId(opts, span.Context);
            Assert.Equal(expected, traceId);

            if (span.Context.TraceOptions.IsSampled)
            {
                Assert.True(request.Headers.Contains(B3Format.X_B3_SAMPLED));
            }
        }

        private HttpResponseMessage GetHttpResponseMessage(HttpStatusCode code)
        {
            var m = new HttpResponseMessage(code);
            m.Headers.Add("TEST", "Header");
            return m;
        }

        private HttpRequestMessage GetHttpRequestMessage()
        {
            var m = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5555/");
            m.Headers.Add("TEST", "Header");
            return m;
        }

        private TracingOptions GetOptions()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:useShortTraceIds"] = "true",
            };
            return GetOptions(appsettings);
        }

        private TracingOptions GetOptions(Dictionary<string, string> settings)
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            TracingOptions opts = new TracingOptions(null, builder.Build());
            return opts;
        }

        private string GetTraceId(TracingOptions options, ISpanContext context)
        {
            var traceId = context.TraceId.ToLowerBase16();
            if (traceId.Length > 16 && options.UseShortTraceIds)
            {
                traceId = traceId.Substring(traceId.Length - 16, 16);
            }

            return traceId;
        }
    }
}

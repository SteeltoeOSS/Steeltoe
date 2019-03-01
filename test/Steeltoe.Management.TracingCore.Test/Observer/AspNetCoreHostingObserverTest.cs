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
using Microsoft.Extensions.Primitives;
using OpenCensus.Trace;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Propagation;
using Steeltoe.Management.Tracing.Test;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class AspNetCoreHostingObserverTest : TestBase
    {
        private static readonly string TRACE_ID_BASE16 = "ff000000000000000000000000000041";
        private static readonly ITraceId TRACE_ID = TraceId.FromLowerBase16(TRACE_ID_BASE16);
        private static readonly string TRACE_ID_BASE16_EIGHT_BYTES = "0000000000000041";
        private static readonly ITraceId TRACE_ID_EIGHT_BYTES = TraceId.FromLowerBase16("0000000000000000" + TRACE_ID_BASE16_EIGHT_BYTES);
        private static readonly string SPAN_ID_BASE16 = "ff00000000000041";
        private static readonly ISpanId SPAN_ID = SpanId.FromLowerBase16(SPAN_ID_BASE16);
        private static readonly byte[] TRACE_OPTIONS_BYTES = new byte[] { 1 };
        private static readonly TraceOptions TRACE_OPTIONS = TraceOptions.FromBytes(TRACE_OPTIONS_BYTES);

        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { HttpContext = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);

            Assert.True(obs.ShouldIgnoreRequest("/cloudfoundryapplication/info"));
            Assert.True(obs.ShouldIgnoreRequest("/cloudfoundryapplication/health"));
            Assert.True(obs.ShouldIgnoreRequest("/foo/bar/image.png"));
            Assert.True(obs.ShouldIgnoreRequest("/foo/bar/image.gif"));
            Assert.True(obs.ShouldIgnoreRequest("/favicon.ico"));
            Assert.True(obs.ShouldIgnoreRequest("/foo.js"));
            Assert.True(obs.ShouldIgnoreRequest("/foo.css"));
            Assert.True(obs.ShouldIgnoreRequest("/javascript/foo.js"));
            Assert.True(obs.ShouldIgnoreRequest("/css/foo.css"));
            Assert.True(obs.ShouldIgnoreRequest("/foo.html"));
            Assert.True(obs.ShouldIgnoreRequest("/html/foo.html"));
            Assert.False(obs.ShouldIgnoreRequest("/api/test"));
            Assert.False(obs.ShouldIgnoreRequest("/v2/apps"));
        }

        [Fact]
        public void ProcessEvent_Stop_NoArgs()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { });
            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_Stop_NothingStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });
            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_Stop_PreviousStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
            var spanContext = obs.Active;
            Assert.NotNull(spanContext);

            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("http:/", span.Name);

            request.Response.StatusCode = (int)HttpStatusCode.OK;
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });

            Assert.True(span.HasEnded);
            Assert.Null(GetCurrentSpan(tracing.Tracer));
            Assert.Null(obs.Active);

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, span.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost:5555"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.response.TEST"]);
            Assert.Equal(AttributeValue.LongAttributeValue((long)HttpStatusCode.OK), attributes[SpanAttributeConstants.HttpStatusCodeKey]);
        }

        [Fact]
        public void ProcessEvent_Exception_NoArgs()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);

            // Null context, Exception
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_EXCEPTION_EVENT, new { });
            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);

            obs.ProcessEvent(AspNetCoreHostingObserver.DIAG_HANDLEDEXCEPTION_EVENT, new { });
            span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);

            obs.ProcessEvent(AspNetCoreHostingObserver.DIAG_UNHANDLEDEXCEPTION_EVENT, new { });
            span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_Exception_NothingStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_EXCEPTION_EVENT, new { httpContext = request, exception = new Exception() });
            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);

            obs.ProcessEvent(AspNetCoreHostingObserver.DIAG_HANDLEDEXCEPTION_EVENT, new { httpContext = request, exception = new Exception() });
            span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);

            obs.ProcessEvent(AspNetCoreHostingObserver.DIAG_UNHANDLEDEXCEPTION_EVENT, new { httpContext = request, exception = new Exception() });
            span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_Exception_PreviousStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
            var spanContext = obs.Active;
            Assert.NotNull(spanContext);

            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("http:/", span.Name);

            var exception = new Exception("Help");
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_EXCEPTION_EVENT, new { httpContext = request, exception = exception });

            request.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });

            Assert.True(span.HasEnded);

            Assert.Null(GetCurrentSpan(tracing.Tracer));
            Assert.Null(obs.Active);

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, span.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost:5555"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.response.TEST"]);
            Assert.Equal(AttributeValue.LongAttributeValue((long)HttpStatusCode.InternalServerError), attributes[SpanAttributeConstants.HttpStatusCodeKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(obs.GetExceptionMessage(exception)), attributes[SpanAttributeConstants.ErrorKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(obs.GetExceptionStackTrace(exception)), attributes[SpanAttributeConstants.ErrorStackTrace]);
        }

        [Fact]
        public void ProcessEvent_Start()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);

            var spanContext = obs.Active;
            Assert.NotNull(spanContext);

            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("http:/", span.Name);

            Assert.False(span.HasEnded);

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, span.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost:5555"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
        }

        [Fact]
        public void ProcessEvent_Start_AllReadyStarted()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);

            var spanContext = obs.Active;
            Assert.NotNull(spanContext);

            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("http:/", span.Name);

            Assert.False(span.HasEnded);

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, span.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost:5555"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);

            var request2 = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request2 });

            span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);

            spanContext = obs.Active;
            Assert.NotNull(spanContext);

            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("http:/", span.Name);

            Assert.False(span.HasEnded);
        }

        [Fact]
        public void ExtractRequestSize()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);

            var request = GetHttpRequestMessage();
            request.Request.Body.WriteByte(1);
            request.Request.Body.WriteByte(2);

            var result = obs.ExtractRequestSize(request);
            Assert.NotNull(result);
            Assert.Equal(2, result.Value);
        }

        [Fact]
        public void ExtractResponseSize()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);

            var request = GetHttpRequestMessage();
            request.Response.Body.WriteByte(1);
            request.Response.Body.WriteByte(2);

            var result = obs.ExtractResponseSize(request);
            Assert.NotNull(result);
            Assert.Equal(2, result.Value);
        }

        [Fact]
        public void ExtractTraceContext()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            request.Request.Headers.Add(B3Constants.XB3TraceId, new StringValues(TRACE_ID_BASE16));
            request.Request.Headers.Add(B3Constants.XB3SpanId, new StringValues(SPAN_ID_BASE16));
            request.Request.Headers.Add(B3Constants.XB3Sampled, new StringValues("1"));

            var context = obs.ExtractTraceContext(request);
            Assert.Equal(TRACE_ID_BASE16, context.TraceId.ToLowerBase16());
            Assert.Equal(SPAN_ID_BASE16, context.SpanId.ToLowerBase16());
            Assert.True(context.TraceOptions.IsSampled);

            request = GetHttpRequestMessage();
            request.Request.Headers.Add(B3Constants.XB3TraceId, new StringValues(TRACE_ID_BASE16_EIGHT_BYTES));
            request.Request.Headers.Add(B3Constants.XB3SpanId, new StringValues(SPAN_ID_BASE16));
            request.Request.Headers.Add(B3Constants.XB3Sampled, new StringValues("1"));

            context = obs.ExtractTraceContext(request);
            Assert.Equal("0000000000000000" + TRACE_ID_BASE16_EIGHT_BYTES, context.TraceId.ToLowerBase16());
            Assert.Equal(SPAN_ID_BASE16, context.SpanId.ToLowerBase16());
            Assert.True(context.TraceOptions.IsSampled);
        }

        private HttpContext GetHttpRequestMessage()
        {
            return GetHttpRequestMessage("GET", "/");
        }

        private HttpContext GetHttpRequestMessage(string method, string path)
        {
            HttpContext context = new DefaultHttpContext();
            context.TraceIdentifier = Guid.NewGuid().ToString();

            context.Request.Body = new MemoryStream();
            context.Response.Body = new MemoryStream();

            context.Request.Headers.Add("TEST", "Header");
            context.Response.Headers.Add("TEST", "Header");

            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";

            context.Request.Host = new HostString("localhost", 5555);
            return context;
        }
    }
}

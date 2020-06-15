// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace.Propagation;
using Steeltoe.Management.Tracing.Test;
using Steeltoe.Management.TracingCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xunit;
using SpanAttributeConstants = Steeltoe.Management.OpenTelemetry.Trace.SpanAttributeConstants;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class AspNetCoreHostingObserverTest : TestBase
    {
        private static readonly string TRACE_ID_BASE16 = "ff000000000000000000000000000041";
        private static readonly string TRACE_ID_BASE16_EIGHT_BYTES = "0000000000000041";
        private static readonly string SPAN_ID_BASE16 = "ff00000000000041";

        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { HttpContext = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
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
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_Stop_NothingStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);

            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_Stop_PreviousStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);

            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
            var spanData = span.ToSpanData();
            Assert.Equal("http:/", spanData.Name);

            request.Response.StatusCode = (int)HttpStatusCode.OK;
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });

            Assert.True(span.HasEnded());
            Assert.Null(GetCurrentSpan(tracing.Tracer));
            Assert.Null(obs.Active);

            spanData = span.ToSpanData();
            var attributes = spanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Server, spanData.Kind);
            Assert.Equal("http://localhost:5555/", attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(HttpMethod.Get.ToString(), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal("localhost:5555", attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal("/", attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal("Header", attributes["http.request.TEST"]);
            Assert.Equal("Header", attributes["http.response.TEST"]);

            Assert.Equal((long)HttpStatusCode.OK, attributes[SpanAttributeConstants.HttpStatusCodeKey]);
        }

        [Fact]
        public void ProcessEvent_Exception_NoArgs()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);

            // Null context, Exception
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_EXCEPTION_EVENT, new { });
            var span = GetCurrentSpan(tracing.Tracer);
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
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_EXCEPTION_EVENT, new { httpContext = request, exception = new Exception() });
            var span = GetCurrentSpan(tracing.Tracer);
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
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
            var spanData = span.ToSpanData();
            Assert.Equal("http:/", spanData.Name);

            var exception = new Exception("Help");
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_EXCEPTION_EVENT, new { httpContext = request, exception = exception });

            request.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });

            Assert.True(span.HasEnded());

            Assert.Null(GetCurrentSpan(tracing.Tracer));
            Assert.Null(obs.Active);

            spanData = span.ToSpanData();
            var attributes = spanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Server, spanData.Kind);
            Assert.Equal("http://localhost:5555/", attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(HttpMethod.Get.ToString(), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal("localhost:5555", attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal("/", attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal("Header", attributes["http.request.TEST"]);
            Assert.Equal("Header", attributes["http.response.TEST"]);
            Assert.Equal((long)HttpStatusCode.InternalServerError, attributes[SpanAttributeConstants.HttpStatusCodeKey]);
            Assert.Equal(obs.GetExceptionMessage(exception), attributes[SpanAttributeConstants.ErrorKey]);
            Assert.Equal(obs.GetExceptionStackTrace(exception), attributes[SpanAttributeConstants.ErrorStackTrace]);
        }

        [Fact]
        public void ProcessEvent_Start()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);

            Assert.Equal("http:/", span.ToSpanData().Name);

            Assert.False(span.HasEnded());

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Server, spanData.Kind);
            Assert.Equal("http://localhost:5555/", attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(HttpMethod.Get.ToString(), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal("localhost:5555", attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal("/", attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal("Header", attributes["http.request.TEST"]);
        }

        [Fact]
        public void ProcessEvent_Start_AllReadyStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);

            var contextSpan = obs.Active;
            Assert.NotNull(contextSpan);

            Assert.Equal(span, contextSpan);
            Assert.Equal("http:/", span.ToSpanData().Name);

            Assert.False(span.HasEnded());

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Server, spanData.Kind);
            Assert.Equal("http://localhost:5555/", attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(HttpMethod.Get.ToString(), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal("localhost:5555", attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal("/", attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal("Header", attributes["http.request.TEST"]);

            var request2 = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request2 });

            span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);

            contextSpan = obs.Active;
            Assert.NotNull(contextSpan);

            Assert.Equal(span, contextSpan);
            Assert.Equal("http:/", span.ToSpanData().Name);

            Assert.False(span.HasEnded());
        }

        [Fact]
        public void ExtractRequestSize()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
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
            var tracing = new OpenTelemetryTracing(opts, null);
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
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            request.Request.Headers.Add(B3Constants.XB3TraceId, new StringValues(TRACE_ID_BASE16));
            request.Request.Headers.Add(B3Constants.XB3SpanId, new StringValues(SPAN_ID_BASE16));
            request.Request.Headers.Add(B3Constants.XB3Sampled, new StringValues("1"));

            var context = obs.ExtractTraceContext(request);
            Assert.Equal(TRACE_ID_BASE16, context.TraceId.ToHexString());
            Assert.Equal(SPAN_ID_BASE16, context.SpanId.ToHexString());
            Assert.True(context.TraceOptions.IsSampled());

            request = GetHttpRequestMessage();
            request.Request.Headers.Add(B3Constants.XB3TraceId, new StringValues(TRACE_ID_BASE16_EIGHT_BYTES));
            request.Request.Headers.Add(B3Constants.XB3SpanId, new StringValues(SPAN_ID_BASE16));
            request.Request.Headers.Add(B3Constants.XB3Sampled, new StringValues("1"));

            context = obs.ExtractTraceContext(request);
            Assert.Equal("0000000000000000" + TRACE_ID_BASE16_EIGHT_BYTES, context.TraceId.ToHexString());
            Assert.Equal(SPAN_ID_BASE16, context.SpanId.ToHexString());
            Assert.True(context.TraceOptions.IsSampled());
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace.Propagation;
using Steeltoe.Management.OpenTelemetryTracingBase.Test;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xunit;
using SpanAttributeConstants = Steeltoe.Management.OpenTelemetry.Trace.SpanAttributeConstants;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class HttpClientDesktopObserverTest : AbstractObserverTest
    {
        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresMissingHttpRequestMessage()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new object());
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { Request = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);

            Assert.True(obs.ShouldIgnoreRequest("/api/v2/spans"));
            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/foobar/permissions"));
            Assert.True(obs.ShouldIgnoreRequest("/v2/apps/barfoo/permissions"));
            Assert.False(obs.ShouldIgnoreRequest("/api/test"));
            Assert.False(obs.ShouldIgnoreRequest("/v2/apps"));
        }

        [Fact]
        public void ProcessEvent_Stop_NoRespose()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientDesktopObserver.STOP_EVENT, new { Request = request });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.False(obs.Pending.TryGetValue(request, out var context));
        }

        [Fact]
        public void ProcessEvent_StopEx_NothingStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientDesktopObserver.STOPEX_EVENT, new { Request = request, StatusCode = HttpStatusCode.OK, Headers = new WebHeaderCollection() });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.False(obs.Pending.TryGetValue(request, out var context));
        }

        [Fact]
        public void ProcessEvent_StopEx_PreviousStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientDesktopObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
            Assert.True(obs.Pending.TryGetValue(request, out var pendingSpan));
            Assert.NotNull(pendingSpan);
            Assert.Equal(span, pendingSpan);
            var spanData = span.ToSpanData();
            Assert.Equal("httpclient:/", spanData.Name);

            var respHeaders = new WebHeaderCollection
            {
                { "TEST", "Header" }
            };

            obs.ProcessEvent(HttpClientDesktopObserver.STOPEX_EVENT, new { Request = request, StatusCode = HttpStatusCode.OK, Headers = respHeaders });

            Assert.True(span.HasEnded());
            Assert.False(obs.Pending.TryGetValue(request, out var pendingSpan2));

            spanData = span.ToSpanData();
            var attributes = spanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);

            Assert.Equal(SpanKind.Client, spanData.Kind);

            Assert.Equal("http://localhost:5555/", attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(HttpMethod.Get.ToString(), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal("localhost:5555", attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal("/", attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal("Header", attributes["http.request.TEST"]);
            Assert.Equal("Header", attributes["http.response.TEST"]);
            Assert.Equal((long)HttpStatusCode.OK, attributes[SpanAttributeConstants.HttpStatusCodeKey]);
        }

        [Fact]
        public void ProcessEvent_Start()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientDesktopObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
            Assert.True(obs.Pending.TryGetValue(request, out var pendingSpan));
            Assert.NotNull(pendingSpan);
            Assert.Equal(span, pendingSpan);
            Assert.Equal("httpclient:/", span.ToSpanData().Name);

            Assert.NotNull(request.Headers.Get(B3Constants.XB3TraceId));
            Assert.NotNull(request.Headers.Get(B3Constants.XB3SpanId));
            Assert.Null(request.Headers.Get(B3Constants.XB3ParentSpanId));

            var spanId = request.Headers.Get(B3Constants.XB3SpanId);
            Assert.Equal(span.Context.SpanId.ToHexString(), spanId);

            var traceId = request.Headers.Get(B3Constants.XB3TraceId);
            var expected = GetTraceId(opts, span.Context);
            Assert.Equal(expected, traceId);

            if (span.IsRecording)
            {
                Assert.NotNull(request.Headers.Get(B3Constants.XB3Sampled));
            }

            Assert.False(span.HasEnded());

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Client, spanData.Kind);
            Assert.Equal("http://localhost:5555/", attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(HttpMethod.Get.ToString(), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal("localhost:5555", attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal("/", attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal("Header", attributes["http.request.TEST"]);
        }

        [Fact]
        public void InjectTraceContext()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientDesktopObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            tracing.Tracer.StartActiveSpan("MySpan", out var span);

            obs.InjectTraceContext(request, null);

            Assert.NotNull(request.Headers.Get(B3Constants.XB3TraceId));
            Assert.NotNull(request.Headers.Get(B3Constants.XB3SpanId));
            Assert.Null(request.Headers.Get(B3Constants.XB3ParentSpanId));

            var spanId = request.Headers.Get(B3Constants.XB3SpanId);
            Assert.Equal(span.ToSpanData().Context.SpanId.ToHexString(), spanId);

            var traceId = request.Headers.Get(B3Constants.XB3TraceId);
            var expected = GetTraceId(opts, span.Context);
            Assert.Equal(expected, traceId);

            if (span.IsRecording)
            {
                Assert.NotNull(request.Headers.Get(B3Constants.XB3Sampled));
            }
        }

        private HttpWebRequest GetHttpRequestMessage()
        {
            var m = WebRequest.CreateHttp("http://localhost:5555/");
            m.Method = HttpMethod.Get.Method;
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
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var opts = new TracingOptions(null, builder.Build());
            return opts;
        }

        private string GetTraceId(TracingOptions options, SpanContext context)
        {
            var traceId = context.TraceId.ToHexString();
            if (traceId.Length > 16 && options.UseShortTraceIds)
            {
                traceId = traceId.Substring(traceId.Length - 16, 16);
            }

            return traceId;
        }
    }
}

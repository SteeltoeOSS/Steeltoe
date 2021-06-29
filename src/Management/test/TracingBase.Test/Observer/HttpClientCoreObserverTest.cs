// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace.Propagation;
using Steeltoe.Management.OpenTelemetryTracingBase.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using SpanAttributeConstants = Steeltoe.Management.OpenTelemetry.Trace.SpanAttributeConstants;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class HttpClientCoreObserverTest : AbstractObserverTest
    {
        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresMissingHttpRequestMessage()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new object());
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { Request = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
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
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
#if NETCOREAPP3_1
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var context));
#else
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#endif
        }

        [Fact]
        public void ProcessEvent_Exception_NothingStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            // No Exception
            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
#if NETCOREAPP3_1
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var context));
#else
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#endif

            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request, Exception = new Exception() });
            span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
#if NETCOREAPP3_1
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out context));
#else
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#endif
        }

        [Fact]
        public void ProcessEvent_Exception_PreviousStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
#if NETCOREAPP3_1
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
            var contextSpan = context as TelemetrySpan;
#else
            var contextSpan = request.Options.FirstOrDefault(o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY).Value;
            Assert.NotNull(contextSpan);
#endif
            Assert.Equal(span, contextSpan);
            Assert.Equal("httpclient:/", span.ToSpanData().Name);

            var exception = new Exception("Help");
            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request, Exception = exception });

            var response = GetHttpResponseMessage(HttpStatusCode.InternalServerError);
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });
            Assert.True(span.HasEnded());
#if NETCOREAPP3_1
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out context));
#else
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#endif

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Assert.Equal(SpanKind.Client, spanData.Kind);
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
        public void ProcessEvent_Stop_PreviousStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
#if NETCOREAPP3_1
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
            var contextSpan = context as TelemetrySpan;
#else
            var contextSpan = request.Options.FirstOrDefault(o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY).Value;
            Assert.NotNull(contextSpan);
#endif
            Assert.NotNull(contextSpan);
            Assert.Equal(span, contextSpan);
            Assert.Equal("httpclient:/", span.ToSpanData().Name);

            var response = GetHttpResponseMessage(HttpStatusCode.OK);
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });
            Assert.True(span.HasEnded());
#if NETCOREAPP3_1
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out context));
#else
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#endif

            var spanData = span.ToSpanData();
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
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
#if NETCOREAPP3_1
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
            var contextSpan = context as TelemetrySpan;
#else
            var contextSpan = request.Options.FirstOrDefault(o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY).Value;
            Assert.NotNull(contextSpan);
#endif

            Assert.NotNull(contextSpan);
            Assert.Equal(span, contextSpan);
            Assert.Equal("httpclient:/", span.ToSpanData().Name);

            Assert.True(request.Headers.Contains(B3Constants.XB3TraceId));
            Assert.True(request.Headers.Contains(B3Constants.XB3SpanId));
            Assert.False(request.Headers.Contains(B3Constants.XB3ParentSpanId));

            var spanId = request.Headers.GetValues(B3Constants.XB3SpanId).Single();
            Assert.Equal(span.Context.SpanId.ToHexString(), spanId);
            var spanData = span.ToSpanData();

            var traceId = request.Headers.GetValues(B3Constants.XB3TraceId).Single();
            var expected = GetTraceId(opts, spanData.Context);
            Assert.Equal(expected, traceId);

            if (span.IsRecording)
            {
                Assert.True(request.Headers.Contains(B3Constants.XB3Sampled));
            }

            Assert.False(span.HasEnded());

            spanData = span.ToSpanData();
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
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            // tracing.Tracer.SpanBuilder("MySpan").StartScopedSpan(out ISpan span);
            tracing.Tracer.StartActiveSpan("MySpan", out var span);

            obs.InjectTraceContext(request, null);

            Assert.True(request.Headers.Contains(B3Constants.XB3TraceId));
            Assert.True(request.Headers.Contains(B3Constants.XB3SpanId));
            Assert.False(request.Headers.Contains(B3Constants.XB3ParentSpanId));

            var spanId = request.Headers.GetValues(B3Constants.XB3SpanId).Single();
            Assert.Equal(span.ToSpanData().Context.SpanId.ToHexString(), spanId);

            var traceId = request.Headers.GetValues(B3Constants.XB3TraceId).Single();
            var expected = GetTraceId(opts, span.Context);
            Assert.Equal(expected, traceId);

            if (span.IsRecording)
            {
                Assert.True(request.Headers.Contains(B3Constants.XB3Sampled));
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

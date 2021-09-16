// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenCensus.Trace;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Propagation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class HttpClientCoreObserverTest : AbstractObserverTest
    {
        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresMissingHttpRequestMessage()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new object());
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { Request = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
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
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
#if NET6_0
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#else
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var context));
#endif
        }

        [Fact]
        public void ProcessEvent_Exception_NothingStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            // No Exception
            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
#if NET6_0
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#else
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var context));
#endif

            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request, Exception = new Exception() });
            span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
#if NET6_0
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#else
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out context));
#endif
        }

        [Fact]
        public void ProcessEvent_Exception_PreviousStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
#if NET6_0
            var context = request.Options.First(o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY).Value;
#else
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out object context));
#endif
            var spanContext = context as HttpClientTracingObserver.SpanContext;
            Assert.NotNull(spanContext);
            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("httpclient:/", span.Name);

            var exception = new Exception("Help");
            obs.ProcessEvent(HttpClientCoreObserver.EXCEPTION_EVENT, new { Request = request, Exception = exception });

            var response = GetHttpResponseMessage(HttpStatusCode.InternalServerError);
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });
            Assert.True(span.HasEnded);
#if NET6_0
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#else
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var ctx));
#endif

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Client, span.Kind);
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
        public void ProcessEvent_Stop_PreviousStarted()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
#if NET6_0
            var context = request.Options.FirstOrDefault(o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY).Value;
#else
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var context));
#endif
            var spanContext = context as HttpClientTracingObserver.SpanContext;
            Assert.NotNull(spanContext);
            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("httpclient:/", span.Name);

            var response = GetHttpResponseMessage(HttpStatusCode.OK);
            obs.ProcessEvent(HttpClientCoreObserver.STOP_EVENT, new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });
            Assert.True(span.HasEnded);
#if NET6_0
            Assert.DoesNotContain(request.Options, o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY);
#else
            Assert.False(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var ctx));
#endif

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Client, span.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost:5555"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.response.TEST"]);
            Assert.Equal(AttributeValue.LongAttributeValue((long)HttpStatusCode.OK), attributes[SpanAttributeConstants.HttpStatusCodeKey]);
        }

        [Fact]
        public void ProcessEvent_Start()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(HttpClientCoreObserver.START_EVENT, new { Request = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(span);
#if NET6_0
            var context = request.Options.FirstOrDefault(o => o.Key == HttpClientCoreObserver.SPANCONTEXT_KEY).Value;
#else
            Assert.True(request.Properties.TryGetValue(HttpClientCoreObserver.SPANCONTEXT_KEY, out var context));
#endif
            var spanContext = context as HttpClientTracingObserver.SpanContext;
            Assert.NotNull(spanContext);
            Assert.Equal(span, spanContext.Active);
            Assert.NotNull(spanContext.ActiveScope);
            Assert.Equal("httpclient:/", span.Name);

            Assert.True(request.Headers.Contains(B3Constants.XB3TraceId));
            Assert.True(request.Headers.Contains(B3Constants.XB3SpanId));
            Assert.False(request.Headers.Contains(B3Constants.XB3ParentSpanId));

            var spanId = request.Headers.GetValues(B3Constants.XB3SpanId).Single();
            Assert.Equal(span.Context.SpanId.ToLowerBase16(), spanId);

            var traceId = request.Headers.GetValues(B3Constants.XB3TraceId).Single();
            var expected = GetTraceId(opts, span.Context);
            Assert.Equal(expected, traceId);

            if (span.Context.TraceOptions.IsSampled)
            {
                Assert.True(request.Headers.Contains(B3Constants.XB3Sampled));
            }

            Assert.False(span.HasEnded);

            var spanData = span.ToSpanData();
            var attributes = spanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Client, span.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), attributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), attributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost:5555"), attributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), attributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), attributes["http.request.TEST"]);
        }

        [Fact]
        public void InjectTraceContext()
        {
            var opts = GetOptions();
            var tracing = new OpenCensusTracing(opts, null);
            var obs = new HttpClientCoreObserver(opts, tracing);
            var request = GetHttpRequestMessage();

            tracing.Tracer.SpanBuilder("MySpan").StartScopedSpan(out var span);

            obs.InjectTraceContext(request, null);

            Assert.True(request.Headers.Contains(B3Constants.XB3TraceId));
            Assert.True(request.Headers.Contains(B3Constants.XB3SpanId));
            Assert.False(request.Headers.Contains(B3Constants.XB3ParentSpanId));

            var spanId = request.Headers.GetValues(B3Constants.XB3SpanId).Single();
            Assert.Equal(span.Context.SpanId.ToLowerBase16(), spanId);

            var traceId = request.Headers.GetValues(B3Constants.XB3TraceId).Single();
            var expected = GetTraceId(opts, span.Context);
            Assert.Equal(expected, traceId);

            if (span.Context.TraceOptions.IsSampled)
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

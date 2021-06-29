// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace;
using Steeltoe.Management.Tracing.Test;
using Steeltoe.Management.TracingCore;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Xunit;
using SpanAttributeConstants = Steeltoe.Management.OpenTelemetry.Trace.SpanAttributeConstants;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class AspNetCoreMvcActionObserverTest : TestBase
    {
        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { HttpContext = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);

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
        public void ProcessEvent_BeforeAction_NoArgs()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { });
            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_BeforeAction_NoCurrentSpan()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            var descriptor = GetActionDescriptor();
            obs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);

            var spanContext = obs.Active;
            Assert.Null(spanContext);
        }

        [Fact]
        public void ProcessEvent_BeforeAction()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var hostobs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            var hostspan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(hostspan);
            var hostSpanContext = hostobs.Active;
            Assert.NotNull(hostSpanContext);
            Assert.Equal(hostspan, hostSpanContext);

            var actionobs = new AspNetCoreMvcActionObserver(opts, tracing);
            var descriptor = GetActionDescriptor();
            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            var actionspan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(actionspan);
            Assert.NotEqual(hostspan, actionspan);
            var actionSpanContext = actionobs.Active;
            Assert.NotNull(actionSpanContext);
            Assert.Equal(actionspan, actionSpanContext);

            // Assert.Equal(actionspan.ParentSpanId, hostspan.Context.SpanId);
            Assert.Equal("action:" + descriptor.ControllerName + "/" + descriptor.ActionName, actionspan.ToSpanData().Name);

            var actionSpanData = actionspan.ToSpanData();
            var actionAttributes = actionSpanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Server, actionSpanData.Kind);
            Assert.Equal(descriptor.ControllerTypeInfo.FullName, actionAttributes[SpanAttributeConstants.MvcControllerClass]);
            Assert.Equal(descriptor.MethodInfo.ToString(), actionAttributes[SpanAttributeConstants.MvcControllerMethod]);
        }

        [Fact]
        public void ProcessEvent_AfterAction_NoBeforeAction()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_AFTERACTION_EVENT, new { httpContext = request });

            var span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);

            var spanContext = obs.Active;
            Assert.Null(spanContext);
        }

        [Fact]
        public void ProcessEvent_AfterAction()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var hostobs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            var hostSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(hostSpan);
            var hostSpanContext = hostobs.Active;
            Assert.NotNull(hostSpanContext);
            Assert.Equal(hostSpan, hostSpanContext);

            var actionobs = new AspNetCoreMvcActionObserver(opts, tracing);
            var descriptor = GetActionDescriptor();
            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            var actionSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(actionSpan);
            Assert.NotEqual(hostSpan, actionSpan);
            var actionSpanContext = actionobs.Active;
            Assert.NotNull(actionSpanContext);
            Assert.Equal(actionSpan, actionSpanContext);

            // Assert.Equal(actionSpan.ParentSpanId, hostSpan.Context.SpanId);
            Assert.Equal("action:" + descriptor.ControllerName + "/" + descriptor.ActionName, actionSpan.ToSpanData().Name);

            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_AFTERACTION_EVENT, new { httpContext = request });

            var spanAfter = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(spanAfter);
            Assert.Equal(hostSpan, spanAfter);

            var actionSpanContextAfter = actionobs.Active;
            Assert.Null(actionSpanContextAfter);
            Assert.True(actionSpan.HasEnded());

            var actionSpanData = actionSpan.ToSpanData();
            var actionAttributes = actionSpanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Server, actionSpanData.Kind);
            Assert.Equal(descriptor.ControllerTypeInfo.FullName, actionAttributes[SpanAttributeConstants.MvcControllerClass]);
            Assert.Equal(descriptor.MethodInfo.ToString(), actionAttributes[SpanAttributeConstants.MvcControllerMethod]);

            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });

            Assert.True(hostSpan.HasEnded());
            Assert.Null(GetCurrentSpan(tracing.Tracer));
            Assert.Null(hostobs.Active);

            var hostSpanData = hostSpan.ToSpanData();
            var hostAttributes = hostSpanData.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.Equal(SpanKind.Server, hostSpanData.Kind);
            Assert.Equal("http://localhost:5555/", hostAttributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(HttpMethod.Get.ToString(), hostAttributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal("localhost:5555", hostAttributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal("/", hostAttributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal("Header", hostAttributes["http.request.TEST"]);
            Assert.Equal("Header", hostAttributes["http.response.TEST"]);
            Assert.Equal((long)HttpStatusCode.OK, hostAttributes[SpanAttributeConstants.HttpStatusCodeKey]);
        }

        [Fact]
        public void ExtractSpanName()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);

            var cdesc = GetActionDescriptor();
            var spanName = obs.ExtractSpanName(cdesc);
            Assert.Equal("action:" + cdesc.ControllerName + "/" + cdesc.ActionName, spanName);

            var desc = new ActionDescriptor()
            {
                DisplayName = "foobar"
            };
            spanName = obs.ExtractSpanName(desc);
            Assert.Equal("action:" + desc.DisplayName, spanName);
        }

        [Fact]
        public void ExtractControllerName()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);

            var cdesc = GetActionDescriptor();
            var spanName = obs.ExtractControllerName(cdesc);
            Assert.Equal(cdesc.ControllerTypeInfo.FullName, spanName);

            var desc = new ActionDescriptor()
            {
                DisplayName = "foobar"
            };
            spanName = obs.ExtractControllerName(desc);
            Assert.Equal("Unknown", spanName);
        }

        [Fact]
        public void ExtractActionName()
        {
            var opts = GetOptions();
            var tracing = new OpenTelemetryTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);

            var cdesc = GetActionDescriptor();
            var spanName = obs.ExtractActionName(cdesc);
            Assert.Equal(cdesc.MethodInfo.ToString(), spanName);

            var desc = new ActionDescriptor()
            {
                DisplayName = "foobar"
            };
            spanName = obs.ExtractActionName(desc);
            Assert.Equal("Unknown", spanName);
        }

        [Fact]
        public void FakeControllerMethod()
        {
        }

        private ControllerActionDescriptor GetActionDescriptor()
        {
            var desc = new ControllerActionDescriptor()
            {
                ControllerName = "foobar",
                ActionName = "barfoo",
                ControllerTypeInfo = GetType().GetTypeInfo(),
                MethodInfo = GetType().GetMethod("FakeControllerMethod")
            };
            return desc;
        }

        private HttpContext GetHttpRequestMessage()
        {
            return GetHttpRequestMessage("GET", "/");
        }

        private HttpContext GetHttpRequestMessage(string method, string path)
        {
            HttpContext context = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString()
            };

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

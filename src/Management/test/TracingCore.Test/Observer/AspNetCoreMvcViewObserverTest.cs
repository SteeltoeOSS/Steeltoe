// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using OpenCensus.Trace;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Tracing.Test;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class AspNetCoreMvcViewObserverTest : TestBase
    {
        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { viewContext = GetViewContext() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);

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
        public void ProcessEvent_BeforeView_NoArgs()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);
            var ctx = GetViewContext();
            obs.ProcessEvent(AspNetCoreMvcViewObserver.MVC_BEFOREVIEW_EVENT, new { });
            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_BeforeView_NoCurrentSpan()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);
            var ctx = GetViewContext();
            obs.ProcessEvent(AspNetCoreMvcViewObserver.MVC_BEFOREVIEW_EVENT, new { viewContext = ctx });

            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);

            var spanContext = obs.Active;
            Assert.Null(spanContext);
        }

        [Fact]
        public void ProcessEvent_BeforeView()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var hostobs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span hostSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(hostSpan);
            var hostSpanContext = hostobs.Active;
            Assert.NotNull(hostSpanContext);
            Assert.Equal(hostSpan, hostSpanContext.Active);

            var actionobs = new AspNetCoreMvcActionObserver(opts, tracing);
            var descriptor = GetActionDescriptor();
            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            Span actionSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(actionSpan);
            Assert.NotEqual(hostSpan, actionSpan);
            var actionContext = actionobs.Active;
            Assert.NotNull(actionContext);
            Assert.Equal(actionSpan, actionContext.Active);
            Assert.Equal(actionSpan.ParentSpanId, hostSpan.Context.SpanId);

            Assert.Equal("action:" + descriptor.ControllerName + "/" + descriptor.ActionName, actionSpan.Name);

            var actionSpanData = actionSpan.ToSpanData();
            var actionAttributes = actionSpanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, actionSpan.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.ControllerTypeInfo.FullName), actionAttributes[SpanAttributeConstants.MvcControllerClass]);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.MethodInfo.ToString()), actionAttributes[SpanAttributeConstants.MvcControllerMethod]);

            var viewobs = new AspNetCoreMvcViewObserver(opts, tracing);
            var ctx = GetViewContext();
            viewobs.ProcessEvent(AspNetCoreMvcViewObserver.MVC_BEFOREVIEW_EVENT, new { viewContext = ctx });

            Span viewSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(viewSpan);
            Assert.NotEqual(hostSpan, viewSpan);
            Assert.NotEqual(actionSpan, viewSpan);
            var viewSpanContext = viewobs.Active;
            Assert.NotNull(viewSpanContext);
            Assert.Equal(viewSpan, viewSpanContext.Active);
            Assert.Equal(actionSpan.Context.SpanId, viewSpan.ParentSpanId);

            Assert.Equal("view:" + ctx.View.Path, viewSpan.Name);

            var viewSpanData = viewSpan.ToSpanData();
            var viewAttributes = viewSpanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, viewSpan.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue(ctx.View.Path), viewAttributes[SpanAttributeConstants.MvcViewFilePath]);
        }

        [Fact]
        public void ProcessEvent_AfterView_NoBeforeView()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);
            obs.ProcessEvent(AspNetCoreMvcViewObserver.MVC_AFTERVIEW_EVENT, new { });

            Span span = GetCurrentSpan(tracing.Tracer);
            Assert.Null(span);

            var spanContext = obs.Active;
            Assert.Null(spanContext);
        }

        [Fact]
        public void ProcessEvent_AfterView()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var hostobs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span hostSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(hostSpan);
            var hostspanContext = hostobs.Active;
            Assert.NotNull(hostspanContext);
            Assert.Equal(hostSpan, hostspanContext.Active);

            var actionobs = new AspNetCoreMvcActionObserver(opts, tracing);
            var descriptor = GetActionDescriptor();
            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            Span actionSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(actionSpan);
            Assert.NotEqual(hostSpan, actionSpan);
            var actionspanContext = actionobs.Active;
            Assert.NotNull(actionspanContext);
            Assert.Equal(actionSpan, actionspanContext.Active);
            Assert.Equal(actionSpan.ParentSpanId, hostSpan.Context.SpanId);

            Assert.Equal("action:" + descriptor.ControllerName + "/" + descriptor.ActionName, actionSpan.Name);

            var viewobs = new AspNetCoreMvcViewObserver(opts, tracing);
            var ctx = GetViewContext();
            viewobs.ProcessEvent(AspNetCoreMvcViewObserver.MVC_BEFOREVIEW_EVENT, new { viewContext = ctx });

            Span viewSpan = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(viewSpan);
            Assert.NotEqual(hostSpan, viewSpan);
            Assert.NotEqual(actionSpan, viewSpan);
            var viewSpanContext = viewobs.Active;
            Assert.NotNull(viewSpanContext);
            Assert.Equal(viewSpan, viewSpanContext.Active);
            Assert.Equal(actionSpan.Context.SpanId, viewSpan.ParentSpanId);

            Assert.Equal("view:" + ctx.View.Path, viewSpan.Name);

            viewobs.ProcessEvent(AspNetCoreMvcViewObserver.MVC_AFTERVIEW_EVENT, new { });

            Assert.True(viewSpan.HasEnded);
            Assert.NotNull(GetCurrentSpan(tracing.Tracer));
            Assert.Null(viewobs.Active);

            var viewSpanData = viewSpan.ToSpanData();
            var viewAttributes = viewSpanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, viewSpan.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue(ctx.View.Path), viewAttributes[SpanAttributeConstants.MvcViewFilePath]);

            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_AFTERACTION_EVENT, new { httpContext = request });

            Span spanAfter = GetCurrentSpan(tracing.Tracer);
            Assert.NotNull(spanAfter);
            Assert.Equal(hostSpan, spanAfter);

            var actionSpanContextAfter = actionobs.Active;
            Assert.Null(actionSpanContextAfter);
            Assert.True(actionSpan.HasEnded);

            var actionSpanData = actionSpan.ToSpanData();
            var actionAttributes = actionSpanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, actionSpan.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.ControllerTypeInfo.FullName), actionAttributes[SpanAttributeConstants.MvcControllerClass]);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.MethodInfo.ToString()), actionAttributes[SpanAttributeConstants.MvcControllerMethod]);

            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });

            Assert.True(hostSpan.HasEnded);
            Assert.Null(GetCurrentSpan(tracing.Tracer));
            Assert.Null(hostobs.Active);

            var hostSpanData = hostSpan.ToSpanData();
            var hostAttributes = hostSpanData.Attributes.AttributeMap;
            Assert.Equal(SpanKind.Server, hostSpan.Kind);
            Assert.Equal(AttributeValue.StringAttributeValue("http://localhost:5555/"), hostAttributes[SpanAttributeConstants.HttpUrlKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(HttpMethod.Get.ToString()), hostAttributes[SpanAttributeConstants.HttpMethodKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("localhost:5555"), hostAttributes[SpanAttributeConstants.HttpHostKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("/"), hostAttributes[SpanAttributeConstants.HttpPathKey]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), hostAttributes["http.request.TEST"]);
            Assert.Equal(AttributeValue.StringAttributeValue("Header"), hostAttributes["http.response.TEST"]);
            Assert.Equal(AttributeValue.LongAttributeValue((long)HttpStatusCode.OK), hostAttributes[SpanAttributeConstants.HttpStatusCodeKey]);
        }

        [Fact]
        public void ExtractSpanName()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);

            var ctx = GetViewContext();
            var spanName = obs.ExtractSpanName(ctx);
            Assert.Equal("view:" + ctx.View.Path, spanName);
        }

        [Fact]
        public void ExtractViewPath()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcViewObserver(opts, tracing);

            var ctx = GetViewContext();
            var path = obs.ExtractViewPath(ctx);
            Assert.Equal(ctx.View.Path, path);
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
                ControllerTypeInfo = this.GetType().GetTypeInfo(),
                MethodInfo = this.GetType().GetMethod("FakeControllerMethod")
            };
            return desc;
        }

        private ViewContext GetViewContext()
        {
            var ctx = new ViewContext()
            {
                HttpContext = GetHttpRequestMessage(),
                View = new TestView()
            };
            return ctx;
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

        private class TestView : IView
        {
            public string Path => "/test/path.cshtml";

            public Task RenderAsync(ViewContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}

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
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using OpenCensus.Trace;
using OpenCensus.Trace.Unsafe;
using Steeltoe.Management.Tracing.Test;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Xunit;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class AspNetCoreMvcActionObserverTest : TestBase
    {
        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            obs.ProcessEvent(null, null);
        }

        [Fact]
        public void ProcessEvent_IgnoresUnknownEvent()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            obs.ProcessEvent(string.Empty, new { HttpContext = GetHttpRequestMessage() });
        }

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
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
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { });
            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.Null(span);
            Assert.Null(obs.Active);
        }

        [Fact]
        public void ProcessEvent_BeforeAction_NoCurrentSpan()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            var descriptor = GetActionDescriptor();
            obs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.Null(span);

            var spanContext = obs.Active;
            Assert.Null(spanContext);
        }

        [Fact]
        public void ProcessEvent_BeforeAction()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var hostobs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span hostspan = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(hostspan);
            var hostSpanContext = hostobs.Active;
            Assert.NotNull(hostSpanContext);
            Assert.Equal(hostspan, hostSpanContext.Active);
            Assert.Null(hostSpanContext.Previous);

            var actionobs = new AspNetCoreMvcActionObserver(opts, tracing);
            var descriptor = GetActionDescriptor();
            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            Span actionspan = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(actionspan);
            Assert.NotEqual(hostspan, actionspan);
            var actionSpanContext = actionobs.Active;
            Assert.NotNull(actionSpanContext);
            Assert.Equal(actionspan, actionSpanContext.Active);
            Assert.Equal(actionspan.ParentSpanId, hostspan.Context.SpanId);
            Assert.NotNull(actionSpanContext.Previous);
            Assert.Equal(hostspan, actionSpanContext.Previous);

            Assert.Equal("action:" + descriptor.ControllerName + "/" + descriptor.ActionName, actionspan.Name);

            var actionSpanData = actionspan.ToSpanData();
            var actionAttributes = actionSpanData.Attributes.AttributeMap;
            Assert.Equal(AttributeValue.StringAttributeValue(SpanAttributeConstants.ServerSpanKind), actionAttributes[SpanAttributeConstants.SpanKindKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.ControllerTypeInfo.FullName), actionAttributes[SpanAttributeConstants.MvcControllerClass]);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.MethodInfo.ToString()), actionAttributes[SpanAttributeConstants.MvcControllerMethod]);
        }

        [Fact]
        public void ProcessEvent_AfterAction_NoBeforeAction()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var obs = new AspNetCoreMvcActionObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            obs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_AFTERACTION_EVENT, new { httpContext = request });

            Span span = AsyncLocalContext.CurrentSpan as Span;
            Assert.Null(span);

            var spanContext = obs.Active;
            Assert.Null(spanContext);
        }

        [Fact]
        public void ProcessEvent_AfterAction()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
            var hostobs = new AspNetCoreHostingObserver(opts, tracing);
            var request = GetHttpRequestMessage();
            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_START_EVENT, new { HttpContext = request });

            Span hostSpan = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(hostSpan);
            var hostSpanContext = hostobs.Active;
            Assert.NotNull(hostSpanContext);
            Assert.Equal(hostSpan, hostSpanContext.Active);
            Assert.Null(hostSpanContext.Previous);

            var actionobs = new AspNetCoreMvcActionObserver(opts, tracing);
            var descriptor = GetActionDescriptor();
            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_BEFOREACTION_EVENT, new { httpContext = request, actionDescriptor = descriptor });

            Span actionSpan = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(actionSpan);
            Assert.NotEqual(hostSpan, actionSpan);
            var actionSpanContext = actionobs.Active;
            Assert.NotNull(actionSpanContext);
            Assert.Equal(actionSpan, actionSpanContext.Active);
            Assert.Equal(actionSpan.ParentSpanId, hostSpan.Context.SpanId);
            Assert.NotNull(actionSpanContext.Previous);
            Assert.Equal(hostSpan, actionSpanContext.Previous);
            Assert.Equal("action:" + descriptor.ControllerName + "/" + descriptor.ActionName, actionSpan.Name);

            actionobs.ProcessEvent(AspNetCoreMvcActionObserver.MVC_AFTERACTION_EVENT, new { httpContext = request });

            Span spanAfter = AsyncLocalContext.CurrentSpan as Span;
            Assert.NotNull(spanAfter);
            Assert.Equal(hostSpan, spanAfter);

            var actionSpanContextAfter = actionobs.Active;
            Assert.Null(actionSpanContextAfter);
            Assert.True(actionSpan.HasEnded);

            var actionSpanData = actionSpan.ToSpanData();
            var actionAttributes = actionSpanData.Attributes.AttributeMap;
            Assert.Equal(AttributeValue.StringAttributeValue(SpanAttributeConstants.ServerSpanKind), actionAttributes[SpanAttributeConstants.SpanKindKey]);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.ControllerTypeInfo.FullName), actionAttributes[SpanAttributeConstants.MvcControllerClass]);
            Assert.Equal(AttributeValue.StringAttributeValue(descriptor.MethodInfo.ToString()), actionAttributes[SpanAttributeConstants.MvcControllerMethod]);

            hostobs.ProcessEvent(AspNetCoreHostingObserver.HOSTING_STOP_EVENT, new { HttpContext = request });

            Assert.True(hostSpan.HasEnded);
            Assert.Null(AsyncLocalContext.CurrentSpan);
            Assert.Null(hostobs.Active);

            var hostSpanData = hostSpan.ToSpanData();
            var hostAttributes = hostSpanData.Attributes.AttributeMap;
            Assert.Equal(AttributeValue.StringAttributeValue(SpanAttributeConstants.ServerSpanKind), hostAttributes[SpanAttributeConstants.SpanKindKey]);
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
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
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
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);
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
                ControllerTypeInfo = this.GetType().GetTypeInfo(),
                MethodInfo = this.GetType().GetMethod("FakeControllerMethod")
            };
            return desc;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointBase.Test.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    public class AspNetCoreHostingObserverTest : BaseTest
    {
        // Pending views API
        /* [Fact]
           public void Constructor_RegistersExpectedViews()
           {
               var options = new MetricsEndpointOptions();
               var stats = new OpenCensusStats();
               var tags = new OpenCensusTags();
               var observer = new AspNetCoreHostingObserver(options, stats, tags, null);

               Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.server.request.time")));
               Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("http.server.request.count")));
           }*/

        [Fact]
        public void ShouldIgnore_ReturnsExpected()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var obs = new AspNetCoreHostingObserver(options, stats, null);

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
        public void ProcessEvent_IgnoresNulls()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new AspNetCoreHostingObserver(options, stats, null);

            observer.ProcessEvent("foobar", null);
            observer.ProcessEvent(AspNetCoreHostingObserver.STOP_EVENT, null);

            Activity act = new Activity("Test");
            act.Start();
            observer.ProcessEvent(AspNetCoreHostingObserver.STOP_EVENT, null);
            act.Stop();
        }

        [Fact]
        public void GetException_ReturnsExpected()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new AspNetCoreHostingObserver(options, stats, null);

            var context = GetHttpRequestMessage();
            string exception = observer.GetException(context);
            Assert.Equal("None", exception);

            context = GetHttpRequestMessage();
            var exceptionHandlerFeature = new ExceptionHandlerFeature()
            {
                Error = new ArgumentNullException()
            };

            context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
            exception = observer.GetException(context);
            Assert.Equal("ArgumentNullException", exception);
        }

        [Fact]
        public void GetLabelSets_ReturnsExpected()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new AspNetCoreHostingObserver(options, stats, null);

            var context = GetHttpRequestMessage();
            var exceptionHandlerFeature = new ExceptionHandlerFeature()
            {
                Error = new ArgumentNullException()
            };

            context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
            context.Response.StatusCode = 404;

            var tagContext = observer.GetLabelSets(context);
            var tagValues = tagContext.ToList();
            tagValues.Contains(KeyValuePair.Create("exception", "ArgumentNullException"));
            tagValues.Contains(KeyValuePair.Create("uri", "/foobar"));
            tagValues.Contains(KeyValuePair.Create("status", "404"));
            tagValues.Contains(KeyValuePair.Create("method", "GET"));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void HandleStopEvent_RecordsStats()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new AspNetCoreHostingObserver(options, stats, null);
            var factory = stats.Factory;
            var processor = stats.Processor;

            var context = GetHttpRequestMessage();
            var exceptionHandlerFeature = new ExceptionHandlerFeature()
            {
                Error = new ArgumentNullException()
            };

            context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
            context.Response.StatusCode = 500;

            Activity act = new Activity("Test");
            act.Start();
            Thread.Sleep(1000);
            act.SetEndTime(DateTime.UtcNow);

            observer.HandleStopEvent(act, context);
            observer.HandleStopEvent(act, context);

            factory.CollectAllMetrics();
            var requestTime = processor.GetMetricByName<double>("http.server.request.time");
            Assert.NotNull(requestTime);
            Assert.Equal(2, requestTime.Count);
            Assert.True(requestTime.Sum / 2 > 1000.00);
            Assert.True(requestTime.Max > 1000.00);

            act.Stop();
        }

        private HttpContext GetHttpRequestMessage()
        {
            return GetHttpRequestMessage("GET", "/foobar");
        }

        private HttpContext GetHttpRequestMessage(string method, string path)
        {
            HttpContext context = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString()
            };

            context.Request.Body = new MemoryStream();
            context.Response.Body = new MemoryStream();

            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";

            context.Request.Host = new HostString("localhost", 5555);
            return context;
        }

        private class ExceptionHandlerFeature : IExceptionHandlerFeature
        {
            public Exception Error { get; set; }
        }
    }
}

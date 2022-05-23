// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Threading;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class TraceDiagnosticObserverTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsOnNulls()
        {
            ITraceOptions options = null;

            var ex2 = Assert.Throws<ArgumentNullException>(() => new TraceDiagnosticObserver(options));
            Assert.Contains(nameof(options), ex2.Message);
        }

        [Fact]
        public void GetSessionId_NoSession_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();
            var result = obs.GetSessionId(context);
            Assert.Null(result);
        }

        [Fact]
        public void GetSessionId_WithSession_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();

            var session = new TestSession();
            ISessionFeature sessFeature = new SessionFeature
            {
                Session = session
            };
            context.Features.Set<ISessionFeature>(sessFeature);

            var result = obs.GetSessionId(context);
            Assert.Equal("TestSessionId", result);
        }

        [Fact]
        public void GetUserPrincipal_NotAuthenticated_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();
            var result = obs.GetUserPrincipal(context);
            Assert.Null(result);
        }

        [Fact]
        public void GetUserPrincipal_Authenticated_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();

            context.User = new ClaimsPrincipal(new MyIdentity());
            var result = obs.GetUserPrincipal(context);
            Assert.Equal("MyTestName", result);
        }

        [Fact]
        public void GetRemoteAddress_NoConnection_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();
            var result = obs.GetRemoteAddress(context);
            Assert.Null(result);
        }

        [Fact]
        public void GetPathInfo_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();

            var result = obs.GetPathInfo(context.Request);
            Assert.Equal("/myPath", result);
        }

        [Fact]
        public void GetRequestUri_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();
            var result = obs.GetRequestUri(context.Request);
            Assert.Equal("http://localhost:1111/myPath", result);
        }

        [Fact]
        public void GetRequestParameters_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();
            var result = obs.GetRequestParameters(context.Request);
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("foo"));
            Assert.True(result.ContainsKey("bar"));
            var fooVal = result["foo"];
            Assert.Single(fooVal);
            Assert.Equal("bar", fooVal[0]);
            var barVal = result["bar"];
            Assert.Single(barVal);
            Assert.Equal("foo", barVal[0]);
        }

        [Fact]
        public void GetTimeTaken_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();
            var time = TimeSpan.FromTicks(10000000);
            var result = obs.GetTimeTaken(time);
            var expected = time.TotalMilliseconds.ToString();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetHeaders_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();

            var result = obs.GetHeaders(100, context.Request.Headers);
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("header1"));
            Assert.True(result.ContainsKey("header2"));
            Assert.True(result.ContainsKey("status"));
            var header1Val = result["header1"] as string;
            Assert.Equal("header1Value", header1Val);
            var header2Val = result["header2"] as string;
            Assert.Equal("header2Value", header2Val);
            var statusVal = result["status"] as string;
            Assert.Equal("100", statusVal);
        }

        [Fact]
        public void GetProperty_NoProperties_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);

            obs.GetProperty(new { foo = "bar" }, out var context);
            Assert.Null(context);
        }

        [Fact]
        public void GetProperty_WithProperties_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var expectedContext = CreateRequest();

            obs.GetProperty(new { HttpContext = expectedContext }, out var context);
            Assert.True(ReferenceEquals(expectedContext, context));
        }

        [Fact]
        public void MakeTrace_ReturnsExpected()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var context = CreateRequest();
            var duration = TimeSpan.FromTicks(20000000 - 10000000);
            var result = obs.MakeTrace(context, duration);
            Assert.NotNull(result);
            Assert.NotNull(result.Info);
            Assert.NotEqual(0, result.TimeStamp);
            Assert.True(result.Info.ContainsKey("method"));
            Assert.True(result.Info.ContainsKey("path"));
            Assert.True(result.Info.ContainsKey("headers"));
            Assert.True(result.Info.ContainsKey("timeTaken"));
            Assert.Equal("GET", result.Info["method"]);
            Assert.Equal("/myPath", result.Info["path"]);
            var headers = result.Info["headers"] as Dictionary<string, object>;
            Assert.NotNull(headers);
            Assert.True(headers.ContainsKey("request"));
            Assert.True(headers.ContainsKey("response"));
            var timeTaken = result.Info["timeTaken"] as string;
            Assert.NotNull(timeTaken);
            var expected = duration.TotalMilliseconds.ToString();
            Assert.Equal(expected, timeTaken);
        }

        [Fact]
        public void ProcessEvent_IgnoresUnprocessableEvents()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);

            // No current activity, event ignored
            obs.ProcessEvent("foobar", null);

            var current = new Activity("barfoo");
            current.Start();

            // Activity current, but no value provided, event ignored
            obs.ProcessEvent("foobar", null);

            // Activity current, value provided, event not stop event, event is ignored
            obs.ProcessEvent("foobar", new object());

            // Activity current, event is stop event, no context in event value, event it ignored
            obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new object());

            Assert.Empty(obs._queue);
            current.Stop();
        }

        [Fact]
        public void Subscribe_Listener_StopActivity_AddsToQueue()
        {
            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            var obs = new TraceDiagnosticObserver(new TraceEndpointOptions());
            obs.Subscribe(listener);

            var context = CreateRequest();
            var activityName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
            var current = new Activity(activityName);

            listener.StartActivity(current, new { HttpContext = context });
            Thread.Sleep(1000);
            listener.StopActivity(current, new { HttpContext = context });

            Assert.Single(obs._queue);
            Assert.True(obs._queue.TryPeek(out var result));
            Assert.NotNull(result.Info);
            Assert.NotEqual(0, result.TimeStamp);
            Assert.True(result.Info.ContainsKey("method"));
            Assert.True(result.Info.ContainsKey("path"));
            Assert.True(result.Info.ContainsKey("headers"));
            Assert.True(result.Info.ContainsKey("timeTaken"));
            Assert.Equal("GET", result.Info["method"]);
            Assert.Equal("/myPath", result.Info["path"]);
            var headers = result.Info["headers"] as Dictionary<string, object>;
            Assert.NotNull(headers);
            Assert.True(headers.ContainsKey("request"));
            Assert.True(headers.ContainsKey("response"));
            var timeTaken = short.Parse(result.Info["timeTaken"] as string);
            Assert.InRange(timeTaken, 1000, 1300);

            obs.Dispose();
            listener.Dispose();
        }

        [Fact]
        public void ProcessEvent_AddsToQueue()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);

            var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            current.Start();

            var context = CreateRequest();

            obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new { HttpContext = context });

            Assert.Single(obs._queue);

            Assert.True(obs._queue.TryPeek(out var result));
            Assert.NotNull(result.Info);
            Assert.NotEqual(0, result.TimeStamp);
            Assert.True(result.Info.ContainsKey("method"));
            Assert.True(result.Info.ContainsKey("path"));
            Assert.True(result.Info.ContainsKey("headers"));
            Assert.True(result.Info.ContainsKey("timeTaken"));
            Assert.Equal("GET", result.Info["method"]);
            Assert.Equal("/myPath", result.Info["path"]);
            var headers = result.Info["headers"] as Dictionary<string, object>;
            Assert.NotNull(headers);
            Assert.True(headers.ContainsKey("request"));
            Assert.True(headers.ContainsKey("response"));
            var timeTaken = result.Info["timeTaken"] as string;
            Assert.NotNull(timeTaken);
            Assert.Equal("0", timeTaken); // 0 because activity not stopped

            current.Stop();
        }

        [Fact]
        public void ProcessEvent_HonorsCapacity()
        {
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            current.Start();

            for (var i = 0; i < 200; i++)
            {
                var context = CreateRequest();
                obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new { HttpContext = context });
            }

            Assert.Equal(option.Capacity, obs._queue.Count);
        }

        [Fact]
        public void GetTraces_ReturnsTraces()
        {
            var listener = new DiagnosticListener("test");
            var option = new TraceEndpointOptions();

            var obs = new TraceDiagnosticObserver(option);
            var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            current.Start();

            for (var i = 0; i < 200; i++)
            {
                var context = CreateRequest();
                obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new { HttpContext = context });
            }

            Assert.Equal(option.Capacity, obs._queue.Count);
            var traces = obs.GetTraces();
            Assert.Equal(option.Capacity, traces.Count);
            Assert.Equal(option.Capacity, obs._queue.Count);

            listener.Dispose();
        }

        private HttpContext CreateRequest()
        {
            HttpContext context = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString()
            };
            context.Response.Body = new MemoryStream();
            context.Request.Method = "GET";
            context.Request.Path = new PathString("/myPath");
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost:1111");
            context.Request.QueryString = new QueryString("?foo=bar&bar=foo");
            context.Request.Headers.Add("Header1", new StringValues("header1Value"));
            context.Request.Headers.Add("Header2", new StringValues("header2Value"));
            return context;
        }
    }
}

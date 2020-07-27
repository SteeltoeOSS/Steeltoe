// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class EndpointMiddlewareTest : BaseTest
    {
        private static readonly Dictionary<string, string> APP_SETTINGS = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Pivotal"] = "Information",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:trace:enabled"] = "true",
        };

        [Fact]
        public async void HandleTraceRequestAsync_ReturnsExpected()
        {
            var opts = new TraceEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);

            var obs = new TraceDiagnosticObserver(opts);
            var ep = new TestTraceEndpoint(opts, obs);
            var middle = new TraceEndpointMiddleware(null, ep, mopts);
            var context = CreateRequest("GET", "/cloudfoundryapplication/httptrace");
            await middle.HandleTraceRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            var json = await rdr.ReadToEndAsync();
            Assert.Equal("[]", json);
        }

        [Fact]
        public async void HandleTraceRequestAsync_OtherPathReturnsExpected()
        {
            var opts = new TraceEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);

            var obs = new TraceDiagnosticObserver(opts);
            var ep = new TestTraceEndpoint(opts, obs);
            var middle = new TraceEndpointMiddleware(null, ep, mopts);
            var context = CreateRequest("GET", "/cloudfoundryapplication/trace");
            await middle.HandleTraceRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            var json = await rdr.ReadToEndAsync();
            Assert.Equal("[]", json);
        }

        [Fact]
        public async void TraceActuator_ReturnsExpectedData()
        {
            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(APP_SETTINGS))
                .ConfigureLogging((webhostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webhostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/trace");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);
        }

        [Fact]
        public void TraceEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new TraceEndpointOptions();
            var mopts = TestHelpers.GetManagementOptions(opts);
            var obs = new TraceDiagnosticObserver(opts);
            var ep = new TraceEndpoint(opts, obs);
            var middle = new TraceEndpointMiddleware(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/trace"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/trace"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
        }

        private HttpContext CreateRequest(string method, string path)
        {
            HttpContext context = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString()
            };
            context.Response.Body = new MemoryStream();
            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            return context;
        }
    }
}
